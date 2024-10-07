using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.Bot
{
    public class ConnectivityTestState
    {
        public DateTime LastTestOn { get; set; }
        public DateTime LastTestSuccessOn { get; set; }
        public TimeSpan LastTestDuration { get; set; }
        public ServiceStatus ServiceStatus { get; set; } = ServiceStatus.Up;
    }
    public class ConnectivityTestsRepository
    {
        private readonly object _lock = new object();
        private Dictionary<string, ConnectivityTestState> States = new Dictionary<string, ConnectivityTestState>();

        public ConnectivityTestState GetOrAddState(string id)
        {
            lock (_lock)
            {
                if (!States.TryGetValue(id, out var state))
                {
                    state = new ConnectivityTestState();
                    States.Add(id, state);

                }
                return state;
            }
        }


    }
    public class Logger : Stormancer.Diagnostics.ILogger
    {
        private readonly ILogger<ConnectivityTest> logger;

        public Logger(ILogger<ConnectivityTest> logger)
        {
            this.logger = logger;
        }
        private LogLevel GetLogLevel(Diagnostics.LogLevel lvl)
        {
            switch (lvl)
            {
                case Diagnostics.LogLevel.Fatal:
                    return LogLevel.Critical;
                case Diagnostics.LogLevel.Error:
                    return LogLevel.Error;
                    
                case Diagnostics.LogLevel.Warn:
                    return LogLevel.Warning;
                    
                case Diagnostics.LogLevel.Info:
                    return LogLevel.Information;
                    
                case Diagnostics.LogLevel.Debug:
                    return LogLevel.Debug;
                    
                case Diagnostics.LogLevel.Trace:
                    return LogLevel.Trace;
                    
            }
            return LogLevel.None;
        }
        public void Log(Diagnostics.LogLevel level, string category, string message, object data = null)
        {
            logger.Log(GetLogLevel(level),$"{category}:{message}",category, message);
        }
    }
    public class ConnectivityTest
    {
        static TimeSpan MAX_DURATION = TimeSpan.FromSeconds(6);
        static int TEST_INTERVAL_MS = 30_000;
        private readonly ILogger<ConnectivityTest> logger;
        private readonly IOptions<BotConfigurationSection> _config;
        private readonly IEnumerable<INotificationChannel> channels;
        private readonly ConnectivityTestsRepository repository;

        public ConnectivityTest(ILogger<ConnectivityTest> logger,IOptions<BotConfigurationSection> config, IEnumerable<INotificationChannel> channels, ConnectivityTestsRepository repository)
        {
            this.logger = logger;
            this._config = config;
            this.channels = channels;
            this.repository = repository;
        }

        private async Task RunConnectivityTestLoop(string id, ApplicationConfigurationSection c, CancellationToken cancellationToken)
        {
            try
            {
                var state = repository.GetOrAddState(id);
                int successiveFailures = 0;

                while (!cancellationToken.IsCancellationRequested)
                {

                    var time = DateTime.Now;

                    async Task<bool> DoTest()
                    {
                        try
                        {
                            logger.Log(LogLevel.Information, "Start connectivity test {id} : {endpoint} {accountId} {appId}", id, c.Endpoint, c.AccountId, c.AppId);
                            var config = Stormancer.ClientConfiguration.Create(c.Endpoint, c.AccountId, c.AppId);
                            config.Plugins.Add(new Stormancer.Plugins.AuthenticationPlugin());
                            config.Plugins.Add(new Stormancer.Plugins.PartyPlugin());
                            config.Plugins.Add(new GameFinderPlugin());
                            config.Logger = new Logger(logger);
                            using var client = new Stormancer.Client(config);
                            var users = client.DependencyResolver.Resolve<Stormancer.Plugins.UserApi>();

                            users.OnGetAuthParameters = () => Task.FromResult(new Stormancer.Plugins.AuthParameters { Type = "ephemeral", Parameters = new Dictionary<string, string> { ["gameVersion.clientVersion"] = c.ClientVersion } });

                            await users.Login();

                            Console.WriteLine("logged in");

                            var party = client.DependencyResolver.Resolve<PartyApi>();

                            await party.CreateParty(new PartyRequestDto { GameFinderName = "matchmaking" });
                            logger.Log(LogLevel.Information, "Completed connectivity test {id} : {endpoint} {accountId} {appId}", id, c.Endpoint, c.AccountId, c.AppId);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            logger.Log(LogLevel.Error,ex, "Failed connectivity test {id} : {endpoint} {accountId} {appId}", id, c.Endpoint, c.AccountId, c.AppId);
                            return false;
                        }
                    }
                    bool success = true;
                    try
                    {
                        success = await DoTest().WaitAsync(TimeSpan.FromSeconds(10));
                    }
                    catch (Exception)
                    {
                       
                        success = false;
                    }


                    state.LastTestDuration = DateTime.Now - time;
                    if (success)
                    {


                        state.LastTestSuccessOn = DateTime.UtcNow;


                        if (state.LastTestDuration > MAX_DURATION)
                        {
                            successiveFailures++;
                            await NotifyDegraded();
                        }
                        else
                        {
                            successiveFailures = 0;
                            await NotifySuccess();
                        }

                    }
                    else
                    {
                        successiveFailures++;
                        await NotifyFailure();
                    }

                    await Task.Delay(TEST_INTERVAL_MS);
                }

                async Task NotifyFailure()
                {
                    if (state.ServiceStatus != ServiceStatus.Down && successiveFailures > 2)
                    {
                        state.ServiceStatus = ServiceStatus.Down;

                        var ctx = new NotificationContext { AppId = id, NewState = state.ServiceStatus };
                        foreach (var channel in channels)
                        {
                            await channel.OnStateChange(ctx);
                        }
                    }
                }

                async Task NotifyDegraded()
                {
                    if (state.ServiceStatus != ServiceStatus.Degraded && successiveFailures > 2)
                    {
                        state.ServiceStatus = ServiceStatus.Degraded;

                        var ctx = new NotificationContext { AppId = id, NewState = state.ServiceStatus };
                        foreach (var channel in channels)
                        {
                            await channel.OnStateChange(ctx);
                        }
                    }
                }
                async Task NotifySuccess()
                {
                    if (state.ServiceStatus != ServiceStatus.Up)
                    {
                        state.ServiceStatus = ServiceStatus.Up;

                        var ctx = new NotificationContext { AppId = id, NewState = state.ServiceStatus };
                        foreach (var channel in channels)
                        {
                            await channel.OnStateChange(ctx);
                        }
                    }
                }

            }
            finally
            {

            }


        }



        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            foreach (var (id, app) in _config.Value.Applications)
            {
                tasks.Add(RunConnectivityTestLoop(id, app, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}

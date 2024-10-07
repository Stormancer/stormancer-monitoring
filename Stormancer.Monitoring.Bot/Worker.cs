using Microsoft.Extensions.Options;

namespace Stormancer.Monitoring.Bot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<BotConfigurationSection> _option;
        private readonly ConnectivityTest test;
        private readonly IEnumerable<INotificationChannel> channels;

        public Worker(ILogger<Worker> logger, IOptions<BotConfigurationSection> option, ConnectivityTest test, IEnumerable<INotificationChannel> channels)
        {
            _logger = logger;
            this._option = option;
            this.test = test;
            this.channels = channels;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>();
            foreach(var channel in channels)
            {
                tasks.Add(channel.RunAsync(stoppingToken));
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                await test.RunAsync(stoppingToken);
            }

            await Task.WhenAll(tasks);
        }

    }
}

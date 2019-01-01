using Stormancer.Monitoring.SmokeTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using MsgPack.Serialization;
using System.Diagnostics;
using Stormancer.Diagnostics;

namespace SmokeTest.ScenarioSample
{

    class StormancerAppWithAuthScenario : IScenario
    {
        public string Name
        {
            get
            {
                return "appAuth";
            }
        }


        public async Task Run(dynamic configuration, string[] args, Action<string, float> sendMetric, Action<string> error)
        {
            bool online = false;
            try
            {

                string endpoint = configuration.app.endpoint;
                string appName = configuration.app.application;
                string appAccount = configuration.app.account;
                //string impersonationKey = configuration.app.impersonationKey;
                //string claimValue = configuration.app.claimValue;
                //string claimPath = configuration.app.claimPath;
                var config = Stormancer.ClientConfiguration.ForAccount(appAccount, appName);
                config.ServerEndpoint = endpoint;
                config.Logger = new DefaultLogger();
                var client = new Stormancer.Client(config);
                var auth = new Authentication(client);
                var id = args.Any() ? args[0] : Guid.NewGuid().ToString();

                await Measure(() =>
                {
                    return auth.Login(id);
                }, "login", sendMetric);

                //var scene = await Measure(() =>
                //{
                //    return auth.Locate("apoc.character", "");
                //}, "connect.locateService", sendMetric);

                online = true;
                client.Disconnect();
            }
            catch (Exception ex)
            {
                error(ex.Message);
                online = false;
            }
            finally
            {
                sendMetric("online", online ? 1f : 0f);
            }
        }

        private async Task Measure(Func<Task> operation, string metricName, Action<string, float> sendMetric)
        {
            var watch = new Stopwatch();
            watch.Start();
            try
            {
                await operation();
            }
            finally
            {
                watch.Stop();
                sendMetric(metricName, watch.ElapsedMilliseconds);
            }

        }

        private async Task<T> Measure<T>(Func<Task<T>> operation, string metricName, Action<string, float> sendMetric)
        {
            var watch = new Stopwatch();
            watch.Start();
            try
            {
                return await operation();
            }
            finally
            {
                watch.Stop();
                sendMetric(metricName, watch.ElapsedMilliseconds);
            }

        }
    }
    public class DefaultLogger : ILogger
    {
        public void Log(LogLevel level, string category, string message, object data)
        {
        }

        public void Log(LogLevel level, string category, string message, Exception ex)
        {
    
        }
    }
}

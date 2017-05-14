using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Stormancer.Management.Client;

namespace Stormancer.Monitoring.SmokeTest.Scenarii
{
    public class CallRpc : IScenario
    {
        public string Name
        {
            get
            {
                return "rpc";
            }
        }

        public async Task<Dictionary<string, float>> Run(dynamic configuration)
        {
            var results = new Dictionary<string, float>();
            try
            {
                var config = ClientConfiguration.ForAccount((string)configuration.account, (string)configuration.application);
                config.ServerEndpoint = configuration.endpoint;

                var stopWatch = new Stopwatch();
                stopWatch.Restart();
                var client = new Client(config);

                var token = await CreateConnectionToken(config.ServerEndpoint, config.Account, config.Application, (string)configuration.scene, (string)configuration.secret);
                var scene = await client.GetScene(token);
                await scene.Connect();
                var connectionTime = stopWatch.ElapsedMilliseconds;
                results.Add("connectionDuration", connectionTime);

                var rpcResults = await scene.Rpc<string, Dictionary<string, float>>((string)configuration.rpc, (string)configuration.secret);
                var rpcTime = stopWatch.ElapsedMilliseconds;
                stopWatch.Stop();


                results.Add("rpcDuration", rpcTime - connectionTime);
                results.Add("online", 1);
                foreach (var r in rpcResults)
                {
                    results.Add(r.Key, r.Value);
                }

            }
            catch (Exception)
            {
                results.Add("online", 0);
            }

            return results;
        }

        private static async Task<string> CreateConnectionToken(string endpoint, string account, string app, string scene, string secret)
        {
            var management = ApplicationClient.ForApi(account, app, secret, endpoint);
            return await management.CreateConnectionToken(scene, "");
        }
    }
}

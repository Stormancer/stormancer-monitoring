using Newtonsoft.Json;
using Stormancer.Management.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Diagnostics;

namespace Stormancer.Monitoring.SmokeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var json = System.IO.File.ReadAllText("config.json");
            var fullConfig = JsonConvert.DeserializeObject<Dictionary<string, Configuration>>(json);

            var tasks = new List<Task<IEnumerable<Tuple<string, float>>>>();
            foreach (var config in fullConfig)
            {
                tasks.Add(RunTest(config.Value).ContinueWith(t =>
                {
                    return t.Result.Select(kvp => Tuple.Create($"{config.Key}.{kvp.Key}", kvp.Value));

                }));

            }

            var results = Task.WhenAll(tasks).Result
               .SelectMany(entries => entries);

            foreach (var result in results)
            {
                Console.WriteLine($"output;{result.Item1};{result.Item2}");
            }

        }

        private static async Task<Dictionary<string, float>> RunTest(Configuration value)
        {
            var results = new Dictionary<string, float>();
            try
            {
                var config = ClientConfiguration.ForAccount(value.account, value.application);
                config.ServerEndpoint = value.endpoint;

                var stopWatch = new Stopwatch();
                stopWatch.Restart();
                var client = new Client(config);

                var token = await CreateConnectionToken(config.ServerEndpoint, config.Account, config.Application, value.scene, value.secret);
                var scene = await client.GetScene(token);
                await scene.Connect();
                var connectionTime = stopWatch.ElapsedMilliseconds;
                results.Add("connectionDuration", connectionTime);

                var rpcResults = await scene.Rpc<string, Dictionary<string, float>>(value.rpc, value.secret);
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

using Newtonsoft.Json;
using Stormancer.Management.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Autofac;
using System.Reflection;

namespace Stormancer.Monitoring.SmokeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var json = System.IO.File.ReadAllText("config.json");
            var fullConfig = JObject.Parse(json);

            var tasks = new List<Task<IEnumerable<Tuple<string, float>>>>();
            foreach (var config in fullConfig)
            {
                tasks.Add(RunTest(config.Value).ContinueWith(t =>
                {
                    return t.Result.Select(kvp => Tuple.Create($"{config.Key}.{kvp.Key}", kvp.Value));

                }));

            }

            Task.WhenAll(tasks).Wait(29000);

            var results = tasks.Where(t => t.IsCompleted).SelectMany(entries => entries.Result);

            foreach (var result in results)
            {
                Console.WriteLine($"output;{result.Item1};{result.Item2}");
            }


        }

        private static async Task<Dictionary<string, float>> RunTest(dynamic config)
        {
            string type = config.__type;

            var mainAssembly = Assembly.GetExecutingAssembly();

            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(mainAssembly)
                .Where(t => t.GetInterfaces().Contains(typeof(IScenario)))
                .AsImplementedInterfaces();

            var container = builder.Build();

            foreach(var scenario in container.Resolve<IEnumerable<IScenario>>())
            {
                if(scenario.Name == type)
                {
                   return await scenario.Run(config);
                }
            }

            return new Dictionary<string, float>();
        }


    }
}

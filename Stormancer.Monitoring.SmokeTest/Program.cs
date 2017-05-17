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

            var tasks = new List<Task>();
            foreach (var config in fullConfig)
            {
                Action<string,float> signalResult = (string id, float result) => {
                    Console.WriteLine($"output;{DateTime.UtcNow};{config.Key}.{id};{result}");
                };
                tasks.Add(RunTest(config.Value, args,signalResult));

            }

            Task.WhenAll(tasks).Wait(29000);


        }

        private static async Task RunTest(dynamic config, string[] args, Action<string,float> sendResult)
        {
            string type = config.__type;

            var mainAssembly = Assembly.GetExecutingAssembly();

            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(mainAssembly)
                .Where(t => t.GetInterfaces().Contains(typeof(IScenario)))
                .AsImplementedInterfaces();
            LoadPlugins(builder);

            var container = builder.Build();

            foreach (var scenario in container.Resolve<IEnumerable<IScenario>>())
            {
                if (scenario.Name == type)
                {
                    
                    await scenario.Run(config, args,sendResult);
                }
            }

           
        }

        private static void LoadPlugins(ContainerBuilder builder)
        {
            var pluginsDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "plugins");

            try
            {
                foreach (var asmPath in System.IO.Directory.EnumerateFiles(pluginsDirectory, "*.dll"))
                {
                    var assembly = Assembly.LoadFile(asmPath);

                    builder.RegisterAssemblyTypes(assembly)
                    .Where(t => t.GetInterfaces().Contains(typeof(IScenario)))
                    .AsImplementedInterfaces();

                }
            }
            catch (Exception) { }
        }


    }
}

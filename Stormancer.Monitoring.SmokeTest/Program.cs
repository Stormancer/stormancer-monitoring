using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
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
                if (config.Key == "timeout")
                {
                    continue;
                }
                Action<string, float> signalResult = (string id, float result) =>
                {
                    Console.WriteLine($"output\t{DateTime.UtcNow}\t{config.Key}.{id}\t{result}");
                };
                Action<string> signalError = (string error) =>
                {
                    Console.WriteLine($"error\t{DateTime.UtcNow}\t{config.Key}\t{error}");
                };
                tasks.Add(RunTest(config.Value, args, signalResult, signalError));

            }

            Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(fullConfig["timeout"]?.ToObject<float?>() ?? 30));


        }

        private static async Task RunTest(dynamic config, string[] args, Action<string, float> sendResult, Action<string> sendError)
        {
            string type = config.__type;

            var mainAssembly = Assembly.GetExecutingAssembly();

            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(mainAssembly)
                .Where(t => IsScenario(t))
                .Named<object>("scenario");
            LoadPlugins(builder);

            var container = builder.Build();
           
            foreach (var scenario in container.ResolveNamed<IEnumerable<object>>("scenario"))
            {
                if (Name(scenario) == type)
                {

                    await Run(scenario,config, args, sendResult, sendError);
                }
            }


        }
        private static string Name(object scenario)
        {
            var t = scenario.GetType();
            var p = t.GetProperty("Name");
            var name =  (string)p.GetGetMethod(false).Invoke(scenario, null);
            return name;
        }
        private static Task Run(object scenario, object config, string[] args, Action<string, float> sendResult, Action<string> error)
        {
            var t = scenario.GetType();

            var run = t.GetMethod("Run");
            if (run.GetParameters().Length == 4)
            {
                return (Task)run.Invoke(scenario, new object[] { config, args, sendResult, error });
            }
            else if (run.GetParameters().Length == 3)
            {
                return (Task)run.Invoke(scenario, new object[] { config, args, sendResult });
            }
            else
            {
                throw new InvalidOperationException($"Failed to start scenario {Name(scenario)}");
            }

        }
        private static bool IsScenario(Type t)
        {
            var run = t.GetMethod("Run");
            if (run == null)
            {
                return false;
            }
            if (!run.ReturnType.IsAssignableTo(typeof(Task)))
            {
                return false;
            }

            var name = t.GetProperty("Name");
            if (name == null)
            {
                return false;
            }

            if (!name.CanRead)
            {
                return false;
            }
            if(name.PropertyType != typeof(string))
            {
                return false;
            }

            var parameters = run.GetParameters();

            if (parameters.Length < 3)
            {
                return false;
            }
            if (parameters[0].ParameterType != typeof(Object))
            {
                return false;
            }
            if (parameters[1].ParameterType != typeof(string[]))
            {
                return false;
            }
            if (parameters[2].ParameterType != typeof(Action<string, float>))
            {
                return false;
            }
            if (parameters.Length >= 4 && parameters[3].ParameterType != typeof(Action<string>))
            {
                return false;
            }
            return true;

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
                        .Where(t => IsScenario(t))
                        .Named<object>("scenario");

                }
            }
            catch (Exception) { }
        }


    }
}

﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.LoadTester
{
    public static class StatisticsExtensions
    {
        public static T Percentile<T>(this IEnumerable<T> values, float percentile)
        {
            var count = values.Count();
            int index = (int)(count * percentile);
            return values.OrderBy(v => v).Skip(index).First();

        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var json = System.IO.File.ReadAllText("scenario.json");
            dynamic config = JObject.Parse(json);

            var startTime = DateTime.UtcNow;
            var duration = (int)config.duration;
            var endDate = startTime + TimeSpan.FromSeconds(duration);

            ConcurrentQueue<DataPoint> results = new ConcurrentQueue<DataPoint>();
            List<User> users = new List<User>();
            var tokenSource = new CancellationTokenSource();

            var tasks = new List<Task>();
            while (DateTime.UtcNow < endDate)
            {
                var missingUsers = ExpectedUserCount(config, DateTime.UtcNow - startTime) - users.Count;
                for (int i = 0; i < missingUsers; i++)
                {
                    var user = new User(d => results.Enqueue(new DataPoint { Time = DateTime.UtcNow - startTime, Data = d }));
                    users.Add(user);
                    tasks.Add(user.Run(config, tokenSource.Token));
                    Console.WriteLine($"Start user {user.Id}");
                }
                results.Enqueue(new DataPoint { Data = new Dictionary<string, float> { { "users", users.Count } }, Time = DateTime.UtcNow - startTime });

                Thread.Sleep(1000);
            }


            tokenSource.Cancel();
            Task.WhenAll(tasks).Wait();

            Console.WriteLine($"Test completed");

            AnalyzeResults(results,config);
        }

        static void AnalyzeResults(ConcurrentQueue<DataPoint> points,dynamic config)
        {
            Console.WriteLine($"Processing results...");
            var sortedPoints = points
                .GroupBy(datapoint => (int)datapoint.Time.TotalSeconds / (int)config.resolution, datapoint => datapoint.Data)
                .Select(datapoint => new
                {
                    Time = datapoint.Key,
                    Data = datapoint
                        .Aggregate(new Dictionary<string, List<float>>(), (acc, value) =>
                            {
                                foreach (var kvp in value)
                                {
                                    if (!acc.ContainsKey(kvp.Key))
                                    {
                                        acc[kvp.Key] = new List<float> { kvp.Value };
                                    }
                                    else
                                    {
                                        acc[kvp.Key].Add(kvp.Value);
                                    }
                                }
                                return acc;
                            })
                        .SelectMany(kvp => ComputeMetrics(kvp.Key, kvp.Value)).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2)
                });

            var metrics = new List<string>();
            foreach (var point in sortedPoints)
            {
                foreach (var metric in point.Data.Keys)
                {
                    if (!metrics.Contains(metric))
                    {
                        metrics.Add(metric);
                    }
                }
            }
            using (var stream = System.IO.File.CreateText("output.csv"))
            {
                stream.Write("time; ");
                stream.Write(string.Join("; ", metrics));
                stream.WriteLine();

                foreach (var point in sortedPoints)
                {
                    stream.Write($"{point.Time*(int)config.resolution}; ");
                    for (int i = 0; i < metrics.Count; i++)
                    {
                        if (point.Data.ContainsKey(metrics[i]))
                        {
                            stream.Write(point.Data[metrics[i]]);
                        }
                        if (i < metrics.Count - 1)
                        {
                            stream.Write("; ");
                        }
                        else
                        {
                            stream.WriteLine();
                        }
                    }
                }
            }

        }

        private static IEnumerable<Tuple<string, float>> ComputeMetrics(string key, IEnumerable<float> values)
        {
            yield return Tuple.Create($"{key}.min", values.Min());
            yield return Tuple.Create($"{key}.max", values.Max());
            yield return Tuple.Create($"{key}.avg", values.Average());
            yield return Tuple.Create($"{key}.90p", values.Percentile(0.9f));
        }



        private static int ExpectedUserCount(dynamic config, TimeSpan elaspedTime)
        {
            return (int)Math.Min(elaspedTime.TotalSeconds * (int)config.users.increase, (int)config.users.max);
        }
    }

    public class DataPoint
    {
        public TimeSpan Time { get; set; }
        public Dictionary<string, float> Data { get; set; }
    }

    class User
    {
        private readonly Action<Dictionary<string, float>> _setResults;
        public string Id { get; }

        public User(Action<Dictionary<string, float>> setResults)
        {
            Id = Guid.NewGuid().ToString();
            _setResults = setResults;
        }

        public async Task Run(dynamic config, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Run(() =>
                {
                    RunScenario(config);
                });
            }
        }

        private void RunScenario(dynamic config)
        {
            Console.WriteLine($"Starting scenario...");
            var results = new Dictionary<string, float>();
            var directory = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            var testExe = System.IO.Path.GetFullPath((string)config.agent);

            if (!File.Exists(testExe))
            {
                Console.WriteLine($"Agent not found at '{testExe}'");
            }
            var psi = new ProcessStartInfo(testExe);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.Arguments = Id;
            psi.WorkingDirectory = System.IO.Path.GetDirectoryName(testExe);
            using (var prc = new Process())
            {
                prc.StartInfo = psi;
                prc.Start();

                prc.WaitForExit();
                var values = prc.StandardOutput.ReadToEnd().Split('\n').Where(line => line.StartsWith("output"));

                foreach (var value in values)
                {
                    var elements = value.Split(';');
                    results.Add(elements[1], float.Parse(elements[2]));
                }


            }
            Console.WriteLine($"Scenario completed");
            _setResults(results);
        }
    }


}

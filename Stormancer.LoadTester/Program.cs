using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.LoadTester
{
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
            while (DateTime.UtcNow > endDate)
            {
                var missingUsers = ExpectedUserCount(config, DateTime.UtcNow - startTime) - users.Count;
                for (int i = 0; i < missingUsers; i++)
                {
                    var user = new User(d => results.Enqueue(new DataPoint { Time = DateTime.UtcNow, Data = d }));
                    tasks.Add(user.Run(tokenSource.Token));
                }

                results.Enqueue(new DataPoint { Data = new Dictionary<string, float> { { "users", users.Count } }, Time = DateTime.UtcNow });
                Thread.Sleep(1000);
            }


            tokenSource.Cancel();
            Task.WhenAll(tasks).Wait();


            AnalyzeResults(results);
        }

        static void AnalyzeResults(ConcurrentQueue<DataPoint> points)
        {


        }

        private static int ExpectedUserCount(dynamic config, TimeSpan elaspedTime)
        {
            return (int)Math.Min(elaspedTime.TotalSeconds * (int)config.increase, (int)config.max);
        }
    }

    public class DataPoint
    {
        public DateTime Time { get; set; }
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

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Run(() => RunScenario());
            }
        }

        private void RunScenario()
        {

            var results = new Dictionary<string, float>();
            var directory = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            var testExe = System.IO.Path.Combine(directory, "smokeTest\\Stormancer.Monitoring.SmokeTest.exe");

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

            _setResults(results);
        }
    }


}

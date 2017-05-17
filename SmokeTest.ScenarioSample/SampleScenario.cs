using Stormancer.Monitoring.SmokeTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTest.ScenarioSample
{
    public class SampleScenario : IScenario
    {
        public string Name
        {
            get
            {
                return "sample";
            }
        }

        public async Task Run(dynamic configuration, string[] args, Action<string, float> sendMetric)
        {
            sendMetric("test", new Random().Next(100));


            await Task.FromResult(true);
        }
    }
}

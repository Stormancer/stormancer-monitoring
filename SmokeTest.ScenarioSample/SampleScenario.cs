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

        public async Task<Dictionary<string, float>> Run(dynamic configuration, string[] args)
        {
            await Task.FromResult(true);

            return new Dictionary<string, float> { { "test", new Random().Next(100) } };
        }
    }
}

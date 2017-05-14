using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.SmokeTest
{
    public interface IScenario
    {
        string Name { get; }
        Task<Dictionary<string, float>> Run(dynamic configuration, string[] args);
    }
}

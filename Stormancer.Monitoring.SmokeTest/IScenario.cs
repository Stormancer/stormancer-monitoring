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
        Task Run(dynamic configuration, string[] args, Action<string,float> sendMetric,Action<string> sendError);
    }
}

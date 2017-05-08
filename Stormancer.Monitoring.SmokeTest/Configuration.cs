using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.SmokeTest
{
    public class Configuration
    {
        public string endpoint { get; set; }
        public string account { get; set; }
        public string application { get; set; }
        public string scene { get; set; }
        public string rpc { get; set; }
        public string secret { get; set; }
    }
}

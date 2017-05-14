using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Monitoring.SD.Plugin
{
    class StormancerCheck : BoxedIce.ServerDensity.Agent.PluginSupport.ICheck
    {
        public string Key
        {
            get
            {
                return "Stormancer";
            }
        }

        public object DoCheck()
        {
            var results = new Dictionary<string, object>();
            var directory =  System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            var testExe = System.IO.Path.Combine(directory, "smokeTest\\Stormancer.Monitoring.SmokeTest.exe");
           
            var psi = new ProcessStartInfo(testExe);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = System.IO.Path.GetDirectoryName(testExe);
            using (var prc = new Process())
            {
                prc.StartInfo = psi;
                prc.Start();

                if (!prc.WaitForExit(30000))
                {
                    prc.Kill();
                }
                else
                {
                    var values = prc.StandardOutput.ReadToEnd().Split('\n').Where(line => line.StartsWith("output"));

                    foreach (var value in values)
                    {
                        var elements = value.Split(';');
                        results.Add(elements[1], float.Parse(elements[2]));
                    }
                }
               
                
            }
            return results;
        }
    }
}

using Stormancer.Monitoring.SmokeTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using MsgPack.Serialization;
using System.Diagnostics;

namespace SmokeTest.ScenarioSample
{
    public class LoginResult
    {
        [MessagePackMember(0)]
        public string ErrorMsg { get; set; } = "";

        [MessagePackMember(1)]
        public bool Success { get; set; }

        [MessagePackMember(2)]
        public string Token { get; set; } = "";

        [MessagePackMember(3)]
        public string UserId { get; set; } = "";

        [MessagePackMember(4)]
        public string Username { get; set; } = "";
    }

    class StormancerAppWithAuthScenario : IScenario
    {
        public string Name
        {
            get
            {
                return "appAuth";
            }
        }

        public async Task Run(dynamic configuration, string[] args, Action<string, float> sendMetric, Action<string> error)
        {
            bool online = false;
            try
            {
                var watch = new Stopwatch();
                watch.Start();
                string endpoint = configuration.app.endpoint;
                string appName = configuration.app.name;
                string appAccount = configuration.app.account;
                string impersonationKey = configuration.app.impersonationKey;
                string claimValue = configuration.app.claimValue;
                string claimPath = configuration.app.claimPath;
                var config = Stormancer.ClientConfiguration.ForAccount(appAccount, appName);
                config.ServerEndpoint = endpoint;

                var client = new Stormancer.Client(config);
                var authenticator = await client.GetPublicScene("authenticator", "");
                await authenticator.Connect();
                watch.Stop();
                sendMetric("connectionTime", watch.ElapsedMilliseconds);
                watch.Restart();
                var result = await authenticator.RpcTask<Dictionary<string, string>, LoginResult>("login", new Dictionary<string, string> {
                    { "provider","impersonation" },
                    { "claimValue",claimValue },
                    {"secret",impersonationKey },
                    {"impersonated-provider","psn" },
                    {"claimPath",claimPath }
                });
                watch.Stop();
                sendMetric("authTime", watch.ElapsedMilliseconds);
                online = result.Success;
                client.Disconnect();
            }
            catch (Exception ex)
            {
                error(ex.Message);
            }
            finally
            {
                sendMetric("online", online ? 1f : 0f);
            }
        }
    }
}

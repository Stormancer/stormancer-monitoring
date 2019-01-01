using MsgPack.Serialization;
using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTest.ScenarioSample
{


    public class Authentication
    {
        private readonly Client _client;
        private Scene _authenticator;

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
        public class AuthParameters
        {
            [MessagePackMember(0)]
            public string Type { get; set; }
            [MessagePackMember(1)]
            public Dictionary<string, string> Parameters { get; set; }
        }

        public Authentication(Client client)
        {
            _client = client;
        }
        public async Task<LoginResult> Login(string deviceIdentifier)
        {
            _authenticator = await _client.GetPublicScene("authenticator", "");
            await _authenticator.Connect();
            var authParameters = new AuthParameters { Type = "deviceidentifier", Parameters = new Dictionary<string, string> { { "deviceidentifier", deviceIdentifier } } };
            return await _authenticator.RpcTask<AuthParameters, LoginResult>("Authentication.Login", authParameters);
        }

        public async Task<Scene> Locate(string type, string name)
        {
            var response = await _authenticator.RpcTask("Locator.GetSceneConnectionToken", s =>
            {
                _authenticator.Host.Serializer().Serialize(type, s);
                _authenticator.Host.Serializer().Serialize(name, s);

            });
            string token;
            using (response.Stream)
            {
                token = _authenticator.Host.Serializer().Deserialize<string>(response.Stream);

            }
            var scene = await _client.GetScene(token);
            await scene.Connect();
            return scene;
        }

    }
}

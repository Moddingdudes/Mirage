using Mirage.Authenticators;

namespace Mirage.Tests.Runtime.Host.Authenticators
{
    public class BasicAuthenticatorHostMode : AuthenticatorHostModeBase
    {
        protected override void AddAuthenticator()
        {
            var auth = this.networkManagerGo.AddComponent<BasicAuthenticator>();
            this.server.authenticator = auth;
            this.client.authenticator = auth;
            auth.serverCode = "1234";
        }
    }
}

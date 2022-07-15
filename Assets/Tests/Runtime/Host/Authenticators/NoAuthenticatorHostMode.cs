namespace Mirage.Tests.Runtime.Host.Authenticators
{
    public class NoAuthenticatorHostMode : AuthenticatorHostModeBase
    {
        protected override void AddAuthenticator()
        {
            this.server.authenticator = null;
            this.client.authenticator = null;

        }
    }
}

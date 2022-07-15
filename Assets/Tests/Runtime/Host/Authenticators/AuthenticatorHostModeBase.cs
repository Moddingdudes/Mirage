using NUnit.Framework;

namespace Mirage.Tests.Runtime.Host.Authenticators
{
    public abstract class AuthenticatorHostModeBase : HostSetup<MockComponent>
    {
        protected abstract void AddAuthenticator();

        private int serverAuthCalled;
        private int clientAuthCalled;

        public sealed override void ExtraSetup()
        {
            this.AddAuthenticator();

            // reset fields
            this.serverAuthCalled = 0;
            this.clientAuthCalled = 0;

            this.server.Authenticated.AddListener(_ => this.serverAuthCalled++);
            this.client.Authenticated.AddListener(_ => this.clientAuthCalled++);
        }

        [Test]
        public void AuthenticatedCalledOnceOnServer()
        {
            Assert.That(this.serverAuthCalled, Is.EqualTo(1));
        }

        [Test]
        public void AuthenticatedCalledOnceOnClient()
        {
            Assert.That(this.clientAuthCalled, Is.EqualTo(1));
        }
    }
}

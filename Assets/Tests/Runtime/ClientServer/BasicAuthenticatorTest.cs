using Mirage.Authenticators;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class BasicAuthenticatorTest : ClientServerSetup<MockComponent>
    {
        private BasicAuthenticator authenticator;

        [Test]
        public void CheckConnected()
        {
            // Should have connected
            Assert.That(this.clientPlayer, Is.Not.Null);
            Assert.That(this.serverPlayer, Is.Not.Null);
        }

        public override void ExtraSetup()
        {
            this.authenticator = this.server.gameObject.AddComponent<BasicAuthenticator>();

            this.server.authenticator = this.authenticator;
            this.client.authenticator = this.authenticator;

            base.ExtraSetup();
        }

    }
}

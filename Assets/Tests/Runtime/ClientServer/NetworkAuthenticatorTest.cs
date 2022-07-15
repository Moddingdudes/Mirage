using System;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.ClientServer
{
    [TestFixture]
    public class NetworkAuthenticatorTest : ClientServerSetup<MockComponent>
    {
        private NetworkAuthenticator serverAuthenticator;
        private NetworkAuthenticator clientAuthenticator;
        private Action<INetworkPlayer> serverMockMethod;
        private Action<INetworkPlayer> clientMockMethod;

        private class NetworkAuthenticationImpl : NetworkAuthenticator
        {
            public override void ClientAuthenticate(INetworkPlayer player) => this.ClientAccept(player);
            public override void ServerAuthenticate(INetworkPlayer player) => this.ServerAccept(player);
            public override void ClientSetup(NetworkClient client) { }
            public override void ServerSetup(NetworkServer server) { }
        }

        public override void ExtraSetup()
        {
            this.serverAuthenticator = this.serverGo.AddComponent<NetworkAuthenticationImpl>();
            this.clientAuthenticator = this.clientGo.AddComponent<NetworkAuthenticationImpl>();
            this.server.authenticator = this.serverAuthenticator;
            this.client.authenticator = this.clientAuthenticator;

            this.serverMockMethod = Substitute.For<Action<INetworkPlayer>>();
            this.serverAuthenticator.OnServerAuthenticated += this.serverMockMethod;

            this.clientMockMethod = Substitute.For<Action<INetworkPlayer>>();
            this.clientAuthenticator.OnClientAuthenticated += this.clientMockMethod;
        }

        [Test]
        public void OnServerAuthenticateTest()
        {
            this.serverAuthenticator.ServerAuthenticate(Substitute.For<INetworkPlayer>());

            this.serverMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void OnClientAuthenticateTest()
        {
            this.clientAuthenticator.ClientAuthenticate(Substitute.For<INetworkPlayer>());

            this.clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void ClientOnValidateTest()
        {
            Assert.That(this.client.authenticator, Is.EqualTo(this.clientAuthenticator));
        }

        [Test]
        public void ServerOnValidateTest()
        {
            Assert.That(this.server.authenticator, Is.EqualTo(this.serverAuthenticator));
        }

        [Test]
        public void NetworkClientCallsAuthenticator()
        {
            this.clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }

        [Test]
        public void NetworkServerCallsAuthenticator()
        {
            this.clientMockMethod.Received().Invoke(Arg.Any<INetworkPlayer>());
        }
    }
}

using NUnit.Framework;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class NetworkIdentityMessageTest : ClientServerSetup<MockComponent>
    {
        [NetworkMessage]
        public struct MyMessage
        {
            public NetworkIdentity player1;
        }

        [Test]
        public void MessageFindsNetworkIdentities()
        {
            NetworkIdentity found = null;
            this.client.MessageHandler.RegisterHandler((MyMessage msg) =>
            {
                found = msg.player1;
            });
            this.serverPlayer.Send(new MyMessage { player1 = this.serverPlayer.Identity });

            this.server.Update();
            this.client.Update();

            Assert.That(found == this.clientPlayer.Identity, "Could not find client version of object");
        }
    }
}

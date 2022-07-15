using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class NetworkClientTest : HostSetup<MockComponent>
    {
        [Test]
        public void IsConnectedTest()
        {
            Assert.That(this.client.IsConnected);
        }

        [Test]
        public void ConnectionTest()
        {
            Assert.That(this.client.Player != null);
        }

        [UnityTest]
        public IEnumerator ClientDisconnectTest() => UniTask.ToCoroutine(async () =>
        {
            this.client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => this.client.connectState == ConnectState.Disconnected);
            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.Active);
        });

        [Test]
        public void ConnectionClearHandlersTest()
        {
            Assert.That(this.ClientMessageHandler.messageHandlers.Count > 0);

            this.ClientMessageHandler.ClearHandlers();

            Assert.That(this.ClientMessageHandler.messageHandlers.Count == 0);
        }

        [Test]
        public void IsLocalClientHostTest()
        {
            Assert.That(this.client.IsLocalClient, Is.True);
        }

        [UnityTest]
        public IEnumerator IsLocalClientShutdownTest() => UniTask.ToCoroutine(async () =>
        {
            this.client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.IsLocalClient);
        });

        [Test]
        public void ConnectedNotNullTest()
        {
            Assert.That(this.client.Connected, Is.Not.Null);
        }

        [Test]
        public void AuthenticatedNotNullTest()
        {
            Assert.That(this.client.Authenticated, Is.Not.Null);
        }

        [Test]
        public void DisconnectedNotNullTest()
        {
            Assert.That(this.client.Disconnected, Is.Not.Null);
        }

        [Test]
        public void TimeNotNullTest()
        {
            Assert.That(this.client.World.Time, Is.Not.Null);
        }
    }
}

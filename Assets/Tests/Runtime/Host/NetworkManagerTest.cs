using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class NetworkManagerTest : HostSetup<MockComponent>
    {
        [Test]
        public void IsNetworkActiveTest()
        {
            Assert.That(this.manager.IsNetworkActive, Is.True);
        }

        [UnityTest]
        public IEnumerator IsNetworkActiveStopTest() => UniTask.ToCoroutine(async () =>
        {
            this.manager.Server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.Active);

            Assert.That(this.server.Active, Is.False);
            Assert.That(this.client.Active, Is.False);
            Assert.That(this.manager.IsNetworkActive, Is.False);
        });

        [UnityTest]
        public IEnumerator StopClientTest() => UniTask.ToCoroutine(async () =>
        {
            this.manager.Client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.Active);
        });

        [Test]
        public void NetworkManagerModeHostTest()
        {
            Assert.That(this.manager.NetworkMode == NetworkManagerMode.Host);
        }

        [UnityTest]
        public IEnumerator NetworkManagerModeOfflineHostTest() => UniTask.ToCoroutine(async () =>
        {
            this.server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active && !this.client.Active);

            Assert.That(this.manager.NetworkMode == NetworkManagerMode.None);
        });
    }
}

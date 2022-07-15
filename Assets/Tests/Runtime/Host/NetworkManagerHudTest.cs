using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class NetworkManagerHudTestSetup : HostSetup<MockComponent>
    {
        private GameObject gameObject;
        protected NetworkManagerHud networkManagerHud;
        public override void ExtraSetup()
        {
            this.gameObject = new GameObject("NetworkManagerHud", typeof(NetworkManagerHud));
            this.networkManagerHud = this.gameObject.GetComponent<NetworkManagerHud>();
            this.networkManagerHud.NetworkManager = this.manager;
            this.networkManagerHud.OfflineGO = new GameObject();
            this.networkManagerHud.OnlineGO = new GameObject();

            //Initial state in the prefab
            this.networkManagerHud.OfflineGO.SetActive(true);
            this.networkManagerHud.OnlineGO.SetActive(false);
        }

        public override void ExtraTearDown()
        {
            Object.DestroyImmediate(this.networkManagerHud.OfflineGO);
            Object.DestroyImmediate(this.networkManagerHud.OnlineGO);
            Object.DestroyImmediate(this.gameObject);
        }
    }

    [TestFixture]
    public class NetworkManagerHudTest : NetworkManagerHudTestSetup
    {
        [Test]
        public void OnlineSetActiveTest()
        {
            this.networkManagerHud.OnlineSetActive();
            Assert.That(this.networkManagerHud.OfflineGO.activeSelf, Is.False);
            Assert.That(this.networkManagerHud.OnlineGO.activeSelf, Is.True);
        }

        [Test]
        public void OfflineSetActiveTest()
        {
            this.networkManagerHud.OfflineSetActive();
            Assert.That(this.networkManagerHud.OfflineGO.activeSelf, Is.True);
            Assert.That(this.networkManagerHud.OnlineGO.activeSelf, Is.False);
        }



        [UnityTest]
        public IEnumerator StopButtonTest() => UniTask.ToCoroutine(async () =>
        {
            this.networkManagerHud.StopButtonHandler();
            Assert.That(this.networkManagerHud.OfflineGO.activeSelf, Is.True);
            Assert.That(this.networkManagerHud.OnlineGO.activeSelf, Is.False);

            await AsyncUtil.WaitUntilWithTimeout(() => !this.manager.IsNetworkActive);

            Assert.That(this.manager.IsNetworkActive, Is.False);
        });
    }

    [TestFixture]
    public class NetworkManagerHudTestNoAutoStart : NetworkManagerHudTestSetup
    {
        protected override bool AutoStartServer => false;

        [Test]
        public void StartServerOnlyButtonTest()
        {
            this.networkManagerHud.StartServerOnlyButtonHandler();
            Assert.That(this.networkManagerHud.OfflineGO.activeSelf, Is.False);
            Assert.That(this.networkManagerHud.OnlineGO.activeSelf, Is.True);

            Assert.That(this.manager.Server.Active, Is.True);
        }
    }
}

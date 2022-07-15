using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.ClientServer
{
    [TestFixture]
    public class NetworkManagerHudClientServerTest : ClientServerSetup<MockComponent>
    {
        protected override bool AutoConnectClient => false;

        private GameObject gameObject;
        private NetworkManagerHud networkManagerHud;
        public override void ExtraSetup()
        {
            this.gameObject = new GameObject("NetworkManagerHud", typeof(NetworkManagerHud));
            this.networkManagerHud = this.gameObject.GetComponent<NetworkManagerHud>();
            this.networkManagerHud.NetworkManager = this.clientGo.AddComponent<NetworkManager>();
            this.networkManagerHud.NetworkManager.Client = this.client;
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

        [Test]
        public void StartClientButtonTest()
        {
            this.networkManagerHud.StartClientButtonHandler();
            Assert.That(this.networkManagerHud.OfflineGO.activeSelf, Is.False);
            Assert.That(this.networkManagerHud.OnlineGO.activeSelf, Is.True);
        }
    }
}

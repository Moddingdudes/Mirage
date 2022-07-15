using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class LobbyReadyTest : HostSetup<MockComponent>
    {
        private GameObject readyPlayer;
        private LobbyReady lobby;
        private ObjectReady readyComp;

        public override void ExtraSetup()
        {
            this.lobby = this.networkManagerGo.AddComponent<LobbyReady>();
        }

        public override void ExtraTearDown()
        {
            this.lobby = null;
        }

        [Test]
        public void SetAllClientsNotReadyTest()
        {
            this.readyComp = this.identity.gameObject.AddComponent<ObjectReady>();
            this.lobby.ObjectReadyList.Add(this.readyComp);
            this.readyComp.IsReady = true;

            this.lobby.SetAllClientsNotReady();

            Assert.That(this.readyComp.IsReady, Is.False);
        }

        [UnityTest]
        public IEnumerator SendToReadyTest() => UniTask.ToCoroutine(async () =>
        {
            this.readyComp = this.identity.gameObject.AddComponent<ObjectReady>();
            this.lobby.ObjectReadyList.Add(this.readyComp);
            this.readyComp.IsReady = true;

            var invokeWovenTestMessage = false;
            this.ClientMessageHandler.RegisterHandler<SceneMessage>(msg => invokeWovenTestMessage = true);
            this.lobby.SendToReady(this.identity, new SceneMessage(), true, Channel.Reliable);

            await AsyncUtil.WaitUntilWithTimeout(() => invokeWovenTestMessage);
        });

        [Test]
        public void IsReadyStateTest()
        {
            this.readyComp = this.identity.gameObject.AddComponent<ObjectReady>();

            Assert.That(this.readyComp.IsReady, Is.False);
        }

        [Test]
        public void SetClientReadyTest()
        {
            this.readyComp = this.identity.gameObject.AddComponent<ObjectReady>();

            this.readyComp.SetClientReady();

            Assert.That(this.readyComp.IsReady, Is.True);
        }

        [Test]
        public void SetClientNotReadyTest()
        {
            this.readyComp = this.identity.gameObject.AddComponent<ObjectReady>();

            this.readyComp.SetClientNotReady();

            Assert.That(this.readyComp.IsReady, Is.False);
        }

        [UnityTest]
        public IEnumerator ClientReadyTest() => UniTask.ToCoroutine(async () =>
        {
            this.readyPlayer = new GameObject();
            this.readyPlayer.AddComponent<NetworkIdentity>();
            this.readyComp = this.readyPlayer.AddComponent<ObjectReady>();

            this.serverObjectManager.Spawn(this.readyPlayer, this.server.LocalPlayer);
            this.readyComp.Ready();

            await AsyncUtil.WaitUntilWithTimeout(() => this.readyComp.IsReady);
        });
    }
}

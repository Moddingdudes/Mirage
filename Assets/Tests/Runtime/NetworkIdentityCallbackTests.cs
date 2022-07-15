using System.Collections.Generic;
using Mirage.Tests.Runtime.ClientServer;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using static Mirage.Tests.LocalConnections;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime
{
    public class NetworkIdentityCallbackTests : ClientServerSetup<MockComponent>
    {
        #region test components
        private class RebuildEmptyObserversNetworkBehaviour : NetworkVisibility
        {
            public override bool OnCheckObserver(INetworkPlayer player) { return true; }
            public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize) { }
        }


        #endregion

        private GameObject gameObject;
        private NetworkIdentity identity;
        private INetworkPlayer player1;
        private INetworkPlayer player2;

        [SetUp]
        public override void ExtraSetup()
        {
            this.gameObject = new GameObject();
            this.identity = this.gameObject.AddComponent<NetworkIdentity>();
            this.identity.Server = this.server;
            this.identity.ServerObjectManager = this.serverObjectManager;

            this.player1 = Substitute.For<INetworkPlayer>();
            this.player2 = Substitute.For<INetworkPlayer>();
        }

        [TearDown]
        public override void ExtraTearDown()
        {
            // set isServer is false. otherwise Destroy instead of
            // DestroyImmediate is called internally, giving an error in Editor
            Object.DestroyImmediate(this.gameObject);
        }


        [Test]
        public void AddAllReadyServerConnectionsToObservers()
        {
            this.player1.SceneIsReady.Returns(true);
            this.player2.SceneIsReady.Returns(false);

            // add some server connections
            this.server.AddTestPlayer(this.player1);
            this.server.AddTestPlayer(this.player2);

            // add a host connection
            this.server.AddLocalConnection(this.client, Substitute.For<SocketLayer.IConnection>());
            this.server.InvokeLocalConnected();
            this.server.LocalPlayer.SceneIsReady = true;

            // call OnStartServer so that observers dict is created
            this.identity.StartServer();

            // add all to observers. should have the two ready connections then.
            this.identity.AddAllReadyServerConnectionsToObservers();
            Assert.That(this.identity.observers, Is.EquivalentTo(new[] { this.player1, this.server.LocalPlayer, this.serverPlayer }));

            // clean up
            this.server.Stop();
        }

        // RebuildObservers should always add the own ready connection
        // (if any). fixes https://github.com/vis2k/Mirror/issues/692
        [Test]
        public void RebuildObserversAddsOwnReadyPlayer()
        {
            // add at least one observers component, otherwise it will just add
            // all server connections
            this.gameObject.AddComponent<RebuildEmptyObserversNetworkBehaviour>();

            // add own player connection
            (var serverPlayer, var _) = PipedConnections(Substitute.For<IMessageReceiver>(), Substitute.For<IMessageReceiver>());
            serverPlayer.SceneIsReady = true;
            this.identity.Owner = serverPlayer;

            // call OnStartServer so that observers dict is created
            this.identity.StartServer();

            // rebuild should at least add own ready player
            this.identity.RebuildObservers(true);
            Assert.That(this.identity.observers, Does.Contain(this.identity.Owner));
        }
    }
}

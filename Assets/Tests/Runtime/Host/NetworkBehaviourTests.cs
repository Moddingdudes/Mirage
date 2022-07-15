using Mirage.Collections;
using NUnit.Framework;
using UnityEngine;

using static Mirage.Tests.LocalConnections;

namespace Mirage.Tests.Runtime.Host
{
    public class SampleBehavior : NetworkBehaviour
    {
    }

    public class NetworkBehaviourTests : HostSetup<SampleBehavior>
    {
        #region Component flags
        [Test]
        public void IsServerOnly()
        {
            Assert.That(this.component.IsServerOnly, Is.False);
        }

        [Test]
        public void IsServer()
        {
            Assert.That(this.component.IsServer, Is.True);
        }

        [Test]
        public void IsClient()
        {
            Assert.That(this.component.IsClient, Is.True);
        }

        [Test]
        public void IsClientOnly()
        {
            Assert.That(this.component.IsClientOnly, Is.False);
        }

        [Test]
        public void PlayerHasAuthorityByDefault()
        {
            // no authority by default
            Assert.That(this.component.HasAuthority, Is.True);
        }

        #endregion

        private class OnStartServerTestComponent : NetworkBehaviour
        {
            public bool called;

            public void OnStartServer()
            {
                Assert.That(this.IsClient, Is.True);
                Assert.That(this.IsLocalPlayer, Is.False);
                Assert.That(this.IsServer, Is.True);
                this.called = true;
            }
        };

        // check isClient/isServer/isLocalPlayer in server-only mode
        [Test]
        public void OnStartServer()
        {
            var gameObject = new GameObject();
            var netIdentity = gameObject.AddComponent<NetworkIdentity>();
            var comp = gameObject.AddComponent<OnStartServerTestComponent>();
            netIdentity.OnStartServer.AddListener(comp.OnStartServer);

            Assert.That(comp.called, Is.False);
            this.serverObjectManager.Spawn(gameObject);

            Assert.That(comp.called, Is.True);

            Object.Destroy(gameObject);
        }


        [Test]
        public void SpawnedObjectNoAuthority()
        {
            var gameObject2 = new GameObject();
            gameObject2.AddComponent<NetworkIdentity>();
            var behaviour2 = gameObject2.AddComponent<SampleBehavior>();

            this.serverObjectManager.Spawn(gameObject2);

            this.client.Update();

            // no authority by default
            Assert.That(behaviour2.HasAuthority, Is.False);
        }

        [Test]
        public void HasIdentitysNetId()
        {
            this.identity.NetId = 42;
            Assert.That(this.component.NetId, Is.EqualTo(42));
        }

        [Test]
        public void HasIdentitysOwner()
        {
            (_, this.identity.Owner) = PipedConnections(this.ClientMessageHandler, this.ServerMessageHandler);
            Assert.That(this.component.Owner, Is.EqualTo(this.identity.Owner));
        }

        [Test]
        public void ComponentIndex()
        {
            var extraObject = new GameObject();

            extraObject.AddComponent<NetworkIdentity>();

            var behaviour1 = extraObject.AddComponent<SampleBehavior>();
            var behaviour2 = extraObject.AddComponent<SampleBehavior>();

            // original one is first networkbehaviour, so index is 0
            Assert.That(behaviour1.ComponentIndex, Is.EqualTo(0));
            // extra one is second networkbehaviour, so index is 1
            Assert.That(behaviour2.ComponentIndex, Is.EqualTo(1));

            Object.Destroy(extraObject);
        }
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourHookGuardTester : NetworkBehaviour
    {
        [Test]
        public void HookGuard()
        {
            // set hook guard for some bits
            for (var i = 0; i < 10; ++i)
            {
                var bit = 1ul << i;

                // should be false by default
                Assert.That(this.GetSyncVarHookGuard(bit), Is.False);

                // set true
                this.SetSyncVarHookGuard(bit, true);
                Assert.That(this.GetSyncVarHookGuard(bit), Is.True);

                // set false again
                this.SetSyncVarHookGuard(bit, false);
                Assert.That(this.GetSyncVarHookGuard(bit), Is.False);
            }
        }
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourInitSyncObjectTester : NetworkBehaviour
    {
        [Test]
        public void InitSyncObject()
        {
            ISyncObject syncObject = new SyncList<bool>();
            this.InitSyncObject(syncObject);
            Assert.That(this.syncObjects.Count, Is.EqualTo(1));
            Assert.That(this.syncObjects[0], Is.EqualTo(syncObject));
        }
    }
}

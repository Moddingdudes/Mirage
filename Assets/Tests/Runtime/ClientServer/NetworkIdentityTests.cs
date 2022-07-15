using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class NetworkIdentityTests : ClientServerSetup<MockComponent>
    {
        [Test]
        public void IsServer()
        {
            Assert.That(this.serverIdentity.IsServer, Is.True);
            Assert.That(this.clientIdentity.IsServer, Is.False);
        }

        [Test]
        public void IsClient()
        {
            Assert.That(this.serverIdentity.IsClient, Is.False);
            Assert.That(this.clientIdentity.IsClient, Is.True);
        }

        [Test]
        public void IsLocalPlayer()
        {
            Assert.That(this.serverIdentity.IsLocalPlayer, Is.False);
            Assert.That(this.clientIdentity.IsLocalPlayer, Is.True);
        }

        [Test]
        public void DefaultAuthority()
        {
            Assert.That(this.serverIdentity.Owner, Is.EqualTo(this.serverPlayer));
            Assert.That(this.clientIdentity.Owner, Is.Null);
        }

        [Test]
        public void ThrowsIfAssignAuthorityCalledOnClient()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.clientIdentity.AssignClientAuthority(this.clientPlayer);
            });
        }

        [Test]
        public void ThrowsIfRemoteAuthorityCalledOnClient()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                // shoud fail because the server is not active
                this.clientIdentity.RemoveClientAuthority();
            });
        }

        [Test]
        public void RemoveAuthority()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                // shoud fail because the server is not active
                this.clientIdentity.RemoveClientAuthority();
            });
        }

        [Test]
        public void IsSceneObject()
        {
            var clone = this.CreateNetworkIdentity();

            clone.SetSceneId(40);
            Assert.That(clone.IsSceneObject, Is.True);
        }
        [Test]
        public void IsNotSceneObject()
        {
            var clone = this.CreateNetworkIdentity();

            clone.SetSceneId(0);
            Assert.That(clone.IsSceneObject, Is.False);
        }
        [Test]
        public void IsPrefab()
        {
            var clone = this.CreateNetworkIdentity();

            clone.PrefabHash = 23232;
            Assert.That(clone.IsPrefab, Is.True);
        }
        [Test]
        public void IsNotPrefab()
        {
            var clone = this.CreateNetworkIdentity();

            clone.PrefabHash = 0;
            Assert.That(clone.IsPrefab, Is.False);
        }
        [Test]
        public void IsNotPrefabIfScenObject()
        {
            var clone = this.CreateNetworkIdentity();

            clone.PrefabHash = 23232;
            clone.SetSceneId(422);
            Assert.That(clone.IsPrefab, Is.False);
        }
        [Test]
        public void IsSpawned()
        {
            var clone = this.CreateNetworkIdentity();
            clone.NetId = 20;

            Assert.That(clone.IsSpawned, Is.True);
        }
        [Test]
        public void IsNotSpawned()
        {
            var clone = this.CreateNetworkIdentity();
            clone.NetId = 0;

            Assert.That(clone.IsSpawned, Is.False);
        }
    }

    public class NetworkIdentityAuthorityTests : ClientServerSetup<MockComponent>
    {
        private NetworkIdentity serverIdentity2;
        private NetworkIdentity clientIdentity2;

        public override async UniTask LateSetup()
        {
            base.ExtraSetup();

            this.serverIdentity2 = GameObject.Instantiate(this.playerPrefab).GetComponent<NetworkIdentity>();
            this.serverObjectManager.Spawn(this.serverIdentity2);

            await UniTask.DelayFrame(2);

            this.client.World.TryGetIdentity(this.serverIdentity2.NetId, out this.clientIdentity2);
            Debug.Assert(this.clientIdentity2 != null);
        }

        public override void ExtraTearDown()
        {
            base.ExtraTearDown();

            if (this.serverIdentity2 != null)
                GameObject.Destroy(this.serverIdentity2);
            if (this.clientIdentity2 != null)
                GameObject.Destroy(this.clientIdentity2);
        }

        [UnityTest]
        public IEnumerator AssignAuthority()
        {
            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);
            Assert.That(this.serverIdentity2.Owner, Is.EqualTo(this.serverPlayer));

            yield return new WaitForSeconds(0.1f);
            Assert.That(this.clientIdentity2.HasAuthority, Is.True);
        }

        [UnityTest]
        public IEnumerator RemoveClientAuthority()
        {
            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);
            yield return new WaitForSeconds(0.1f);

            this.serverIdentity2.RemoveClientAuthority();
            Assert.That(this.serverIdentity2.Owner, Is.EqualTo(null));

            yield return new WaitForSeconds(0.1f);
            Assert.That(this.clientIdentity2.HasAuthority, Is.False);
        }

        [UnityTest]
        public IEnumerator RemoveClientAuthority_DoesNotResetPosition()
        {
            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);
            yield return new WaitForSeconds(0.1f);

            // set position on client
            var clientPosition = new Vector3(200, -30, 40);
            this.clientIdentity2.transform.position = clientPosition;
            this.serverIdentity2.transform.position = Vector3.zero;

            // remove auth on server
            this.serverIdentity2.RemoveClientAuthority();

            yield return new WaitForSeconds(0.1f);
            // expect authority to be gone, but position not to be reset
            Debug.Assert(this.clientIdentity2.HasAuthority == false);
            Assert.That(this.clientIdentity2.transform.position, Is.EqualTo(clientPosition));
            Assert.That(this.serverIdentity2.transform.position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        [Description("OnAuthorityChanged should not be called on server side")]
        public void OnAuthorityChanged_Server()
        {
            var hasAuthCalls = new Queue<bool>();
            this.serverIdentity2.OnAuthorityChanged.AddListener(hasAuth =>
            {
                hasAuthCalls.Enqueue(hasAuth);
            });

            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);

            Assert.That(hasAuthCalls.Count, Is.EqualTo(0));

            this.serverIdentity2.RemoveClientAuthority();

            Assert.That(hasAuthCalls.Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator OnAuthorityChanged_Client()
        {
            var hasAuthCalls = new Queue<bool>();
            this.clientIdentity2.OnAuthorityChanged.AddListener(hasAuth =>
            {
                hasAuthCalls.Enqueue(hasAuth);
            });

            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);
            yield return new WaitForSeconds(0.1f);

            Assert.That(hasAuthCalls.Count, Is.EqualTo(1));
            Assert.That(hasAuthCalls.Dequeue(), Is.True);

            this.serverIdentity2.RemoveClientAuthority();
            yield return new WaitForSeconds(0.1f);

            Assert.That(hasAuthCalls.Count, Is.EqualTo(1));
            Assert.That(hasAuthCalls.Dequeue(), Is.False);
        }

        [Test]
        public void OnOwnerChanged_Server()
        {
            var hasAuthCalls = new Queue<INetworkPlayer>();
            this.serverIdentity2.OnOwnerChanged.AddListener(newOwner =>
            {
                hasAuthCalls.Enqueue(newOwner);
            });

            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);

            Assert.That(hasAuthCalls.Count, Is.EqualTo(1));
            Assert.That(hasAuthCalls.Dequeue(), Is.EqualTo(this.serverPlayer));

            this.serverIdentity2.RemoveClientAuthority();

            Assert.That(hasAuthCalls.Count, Is.EqualTo(1));
            Assert.That(hasAuthCalls.Dequeue(), Is.Null);
        }

        [UnityTest]
        [Description("OnOwnerChanged should not be called on client side")]
        public IEnumerator OnOwnerChanged_Client()
        {
            var hasAuthCalls = new Queue<INetworkPlayer>();
            this.clientIdentity2.OnOwnerChanged.AddListener(newOwner =>
            {
                hasAuthCalls.Enqueue(newOwner);
            });

            this.serverIdentity2.AssignClientAuthority(this.serverPlayer);
            yield return new WaitForSeconds(0.1f);

            Assert.That(hasAuthCalls.Count, Is.EqualTo(0));

            this.serverIdentity2.RemoveClientAuthority();
            yield return new WaitForSeconds(0.1f);

            Assert.That(hasAuthCalls.Count, Is.EqualTo(0));
        }
    }
}

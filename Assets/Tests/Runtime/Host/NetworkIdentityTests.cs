using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using InvalidOperationException = System.InvalidOperationException;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.Host
{
    public class NetworkIdentityTests : HostSetup<MockComponent>
    {
        #region SetUp

        private GameObject gameObject;
        private NetworkIdentity testIdentity;

        public override void ExtraSetup()
        {
            this.gameObject = new GameObject();
            this.testIdentity = this.gameObject.AddComponent<NetworkIdentity>();
        }

        public override void ExtraTearDown()
        {
            Object.Destroy(this.gameObject);
        }

        #endregion

        [Test]
        public void AssignClientAuthorityNoServer()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.testIdentity.AssignClientAuthority(this.server.LocalPlayer);
            });
        }

        [Test]
        public void IsServer()
        {
            Assert.That(this.testIdentity.IsServer, Is.False);
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);

            Assert.That(this.testIdentity.IsServer, Is.True);
        }

        [Test]
        public void IsClient()
        {
            Assert.That(this.testIdentity.IsClient, Is.False);
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);

            Assert.That(this.testIdentity.IsClient, Is.True);
        }

        [Test]
        public void IsLocalPlayer()
        {
            Assert.That(this.testIdentity.IsLocalPlayer, Is.False);
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);

            Assert.That(this.testIdentity.IsLocalPlayer, Is.False);
        }

        [Test]
        public void DefaultAuthority()
        {
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);
            Assert.That(this.testIdentity.Owner, Is.Null);
        }

        [Test]
        public void AssignAuthority()
        {
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);
            this.testIdentity.AssignClientAuthority(this.server.LocalPlayer);

            Assert.That(this.testIdentity.Owner, Is.SameAs(this.server.LocalPlayer));
        }

        [Test]
        public void SpawnWithAuthority()
        {
            this.serverObjectManager.Spawn(this.gameObject, this.server.LocalPlayer);
            Assert.That(this.testIdentity.Owner, Is.SameAs(this.server.LocalPlayer));
        }

        [Test]
        public void SpawnWithPrefabHash()
        {
            var hash = Guid.NewGuid().GetHashCode();
            this.serverObjectManager.Spawn(this.gameObject, hash, this.server.LocalPlayer);
            Assert.That(this.testIdentity.PrefabHash, Is.EqualTo(hash));
        }

        [Test]
        public void ReassignClientAuthority()
        {
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);
            // assign authority
            this.testIdentity.AssignClientAuthority(this.server.LocalPlayer);

            // shouldn't be able to assign authority while already owned by
            // another connection
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.testIdentity.AssignClientAuthority(Substitute.For<INetworkPlayer>());
            });
        }

        [Test]
        public void AssignNullAuthority()
        {
            // create a networkidentity with our test component
            this.serverObjectManager.Spawn(this.gameObject);

            // someone might try to remove authority by assigning null.
            // make sure this fails.
            Assert.Throws<ArgumentNullException>(() =>
            {
                this.testIdentity.AssignClientAuthority(null);
            });
        }

        [Test]
        public void RemoveclientAuthorityNotSpawned()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                // shoud fail because the server is not active
                this.testIdentity.RemoveClientAuthority();
            });
        }

        [Test]
        public void RemoveClientAuthorityOfOwner()
        {
            this.serverObjectManager.ReplaceCharacter(this.server.LocalPlayer, this.gameObject);

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.testIdentity.RemoveClientAuthority();
            });
        }

        [Test]
        public void RemoveClientAuthority()
        {
            this.serverObjectManager.Spawn(this.gameObject);
            this.testIdentity.AssignClientAuthority(this.server.LocalPlayer);
            this.testIdentity.RemoveClientAuthority();
            Assert.That(this.testIdentity.Owner, Is.Null);
            Assert.That(this.testIdentity.HasAuthority, Is.False);
            Assert.That(this.testIdentity.IsLocalPlayer, Is.False);
        }

        [UnityTest]
        public IEnumerator OnStopServer() => UniTask.ToCoroutine(async () =>
        {
            this.serverObjectManager.Spawn(this.gameObject);

            var mockHandler = Substitute.For<UnityAction>();
            this.testIdentity.OnStopServer.AddListener(mockHandler);

            this.serverObjectManager.Destroy(this.gameObject, false);

            await UniTask.Delay(1);
            mockHandler.Received().Invoke();
        });

        [Test]
        public void IdentityClientValueSet()
        {
            Assert.That(this.identity.Client, Is.Not.Null);
        }

        [Test]
        public void IdentityServerValueSet()
        {
            Assert.That(this.identity.Server, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DestroyOwnedObjectsTest() => UniTask.ToCoroutine(async () =>
        {
            var testObj1 = new GameObject().AddComponent<NetworkIdentity>();
            var testObj2 = new GameObject().AddComponent<NetworkIdentity>();
            var testObj3 = new GameObject().AddComponent<NetworkIdentity>();

            // only destroys spawned objects, so spawn them here
            this.serverObjectManager.Spawn(testObj1);
            this.serverObjectManager.Spawn(testObj2);
            this.serverObjectManager.Spawn(testObj3);

            this.server.LocalPlayer.AddOwnedObject(testObj1);
            this.server.LocalPlayer.AddOwnedObject(testObj2);
            this.server.LocalPlayer.AddOwnedObject(testObj3);
            this.server.LocalPlayer.DestroyOwnedObjects();

            await AsyncUtil.WaitUntilWithTimeout(() => !testObj1);
            await AsyncUtil.WaitUntilWithTimeout(() => !testObj2);
            await AsyncUtil.WaitUntilWithTimeout(() => !testObj3);
        });
    }

    public class NetworkIdentityStartedTests : HostSetup<MockComponent>
    {
        #region SetUp

        private GameObject gameObject;
        private NetworkIdentity testIdentity;

        public override void ExtraSetup()
        {
            this.gameObject = new GameObject();
            this.testIdentity = this.gameObject.AddComponent<NetworkIdentity>();
            this.server.Started.AddListener(() => this.serverObjectManager.Spawn(this.gameObject));
        }

        public override void ExtraTearDown()
        {
            Object.Destroy(this.gameObject);
        }

        #endregion

        [UnityTest]
        public IEnumerator ClientNotNullAfterSpawnInStarted() => UniTask.ToCoroutine(async () =>
        {
            await AsyncUtil.WaitUntilWithTimeout(() => (this.testIdentity.Client as NetworkClient) == this.client);
        });
    }
}

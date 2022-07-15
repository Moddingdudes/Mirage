using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    [TestFixture]
    public class ServerObjectManagerTests : ClientServerSetup<MockComponent>
    {
        private void AssertNoIdentityMessage(InvalidOperationException ex, string name) => Assert.That(ex.Message, Is.EqualTo($"Gameobject {name} doesn't have NetworkIdentity."));
        private void AssertNoIdentityMessage(InvalidOperationException ex) => this.AssertNoIdentityMessage(ex, new GameObject().name);

        private NetworkIdentity CreatePlayerReplacement()
        {
            var playerReplacement = new GameObject("replacement", typeof(NetworkIdentity));
            this.toDestroy.Add(playerReplacement);
            var replacementIdentity = playerReplacement.GetComponent<NetworkIdentity>();
            replacementIdentity.PrefabHash = Guid.NewGuid().GetHashCode();
            this.clientObjectManager.RegisterPrefab(replacementIdentity);

            return replacementIdentity;
        }

        [Test]
        public void ThrowsIfSpawnCalledWhenServerIsNotAcctive()
        {
            var obj = new GameObject();

            this.server.Stop();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.Spawn(new GameObject().AddComponent<NetworkIdentity>(), this.serverPlayer);
            });

            Assert.That(ex.Message, Is.EqualTo("NetworkServer is not active. Cannot spawn objects without an active server."));
            GameObject.DestroyImmediate(obj);
        }

        [Test]
        public void ThrowsIfSpawnCalledOwnerHasNoNetworkIdentity()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.Spawn(new GameObject(), new GameObject());
            });

            this.AssertNoIdentityMessage(ex);
        }

        [UnityTest]
        public IEnumerator SpawnByIdentityTest() => UniTask.ToCoroutine(async () =>
        {
            this.serverObjectManager.Spawn(this.serverIdentity);

            await AsyncUtil.WaitUntilWithTimeout(() => (NetworkServer)this.serverIdentity.Server == this.server);
        });

        [Test]
        public void ThrowsIfSpawnCalledWithOwnerWithNoOwnerTest()
        {
            var badOwner = new GameObject();
            badOwner.AddComponent<NetworkIdentity>();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.Spawn(new GameObject(), badOwner);
            });

            Assert.That(ex.Message, Is.EqualTo("Player object is not a player in the connection"));
            GameObject.DestroyImmediate(badOwner);
        }

        [UnityTest]
        public IEnumerator ShowForPlayerTest() => UniTask.ToCoroutine(async () =>
        {
            var invoked = false;

            this.ClientMessageHandler.RegisterHandler<SpawnMessage>(msg => invoked = true);

            this.serverPlayer.SceneIsReady = true;

            // call ShowForConnection
            this.serverObjectManager.ShowToPlayer(this.serverIdentity, this.serverPlayer);

            // todo assert correct message was sent using Substitute for socket or player

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [Test]
        public void SpawnSceneObject()
        {
            var sceneObject = this.InstantiateForTest(this.playerPrefab).GetComponent<NetworkIdentity>();
            sceneObject.SetSceneId(42);

            Debug.Assert(!sceneObject.IsSpawned, "Identity should be unspawned for this test");
            this.serverObjectManager.SpawnObjects();
            Assert.That(sceneObject.NetId, Is.Not.Zero);
        }

        [Test]
        public void DoesNotSpawnNonSceneObject()
        {
            var sceneObject = this.InstantiateForTest(this.playerPrefab).GetComponent<NetworkIdentity>();
            sceneObject.SetSceneId(0);

            Debug.Assert(!sceneObject.IsSpawned, "Identity should be unspawned for this test");
            this.serverObjectManager.SpawnObjects();
            Assert.That(sceneObject.NetId, Is.Zero);
        }

        [Test]
        public void SpawnEvent()
        {
            var mockHandler = Substitute.For<Action<NetworkIdentity>>();
            this.server.World.onSpawn += mockHandler;
            var newObj = GameObject.Instantiate(this.playerPrefab);
            this.serverObjectManager.Spawn(newObj);

            mockHandler.Received().Invoke(Arg.Any<NetworkIdentity>());
            this.serverObjectManager.Destroy(newObj);
        }

        [UnityTest]
        public IEnumerator ClientSpawnEvent() => UniTask.ToCoroutine(async () =>
        {
            var mockHandler = Substitute.For<Action<NetworkIdentity>>();
            this.client.World.onSpawn += mockHandler;
            var newObj = GameObject.Instantiate(this.playerPrefab);
            this.serverObjectManager.Spawn(newObj);

            await UniTask.WaitUntil(() => mockHandler.ReceivedCalls().Any()).Timeout(TimeSpan.FromMilliseconds(200));

            mockHandler.Received().Invoke(Arg.Any<NetworkIdentity>());
            this.serverObjectManager.Destroy(newObj);
        });

        [UnityTest]
        public IEnumerator ClientUnSpawnEvent() => UniTask.ToCoroutine(async () =>
        {
            var mockHandler = Substitute.For<Action<NetworkIdentity>>();
            this.client.World.onUnspawn += mockHandler;
            var newObj = GameObject.Instantiate(this.playerPrefab);
            this.serverObjectManager.Spawn(newObj);
            this.serverObjectManager.Destroy(newObj);

            await UniTask.WaitUntil(() => mockHandler.ReceivedCalls().Any()).Timeout(TimeSpan.FromMilliseconds(200));
            mockHandler.Received().Invoke(Arg.Any<NetworkIdentity>());
        });

        [Test]
        public void UnSpawnEvent()
        {
            var mockHandler = Substitute.For<Action<NetworkIdentity>>();
            this.server.World.onUnspawn += mockHandler;
            var newObj = GameObject.Instantiate(this.playerPrefab);
            this.serverObjectManager.Spawn(newObj);
            this.serverObjectManager.Destroy(newObj);
            mockHandler.Received().Invoke(newObj.GetComponent<NetworkIdentity>());
        }

        [Test]
        public void ReplacePlayerBaseTest()
        {
            var replacement = this.CreatePlayerReplacement();

            this.serverObjectManager.ReplaceCharacter(this.serverPlayer, replacement);

            Assert.That(this.serverPlayer.Identity, Is.EqualTo(replacement));
        }

        [Test]
        public void ReplacePlayerDontKeepAuthTest()
        {
            var replacement = this.CreatePlayerReplacement();

            this.serverObjectManager.ReplaceCharacter(this.serverPlayer, replacement, true);

            Assert.That(this.clientIdentity.Owner, Is.EqualTo(null));
        }

        [Test]
        public void ReplacePlayerPrefabHashTest()
        {
            var replacement = this.CreatePlayerReplacement();
            var hash = replacement.PrefabHash;

            this.serverObjectManager.ReplaceCharacter(this.serverPlayer, replacement, hash);

            Assert.That(this.serverPlayer.Identity.PrefabHash, Is.EqualTo(hash));
        }

        [Test]
        public void AddPlayerForConnectionPrefabHashTest()
        {
            var replacement = this.CreatePlayerReplacement();
            var hash = replacement.PrefabHash;

            this.serverPlayer.Identity = null;

            this.serverObjectManager.AddCharacter(this.serverPlayer, replacement, hash);

            Assert.That(replacement == this.serverPlayer.Identity);
        }

        [UnityTest]
        public IEnumerator DestroyCharacter() => UniTask.ToCoroutine(async () =>
        {
            this.serverObjectManager.DestroyCharacter(this.serverPlayer);

            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(this.serverPlayerGO == null);
            Assert.That(this.clientPlayerGO == null);
        });

        [UnityTest]
        public IEnumerator DestroyCharacterKeepServerObject() => UniTask.ToCoroutine(async () =>
        {
            this.serverObjectManager.DestroyCharacter(this.serverPlayer, destroyServerObject: false);

            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(this.serverPlayerGO != null);
            Assert.That(this.clientPlayerGO == null);
        });

        [UnityTest]
        public IEnumerator RemoveCharacterKeepAuthority() => UniTask.ToCoroutine(async () =>
        {
            this.serverObjectManager.RemoveCharacter(this.serverPlayer, true);

            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(this.serverPlayerGO != null);
            Assert.That(this.serverIdentity.Owner == this.serverPlayer);

            Assert.That(this.clientPlayerGO != null);
            Assert.That(this.clientIdentity.HasAuthority, Is.True);
            Assert.That(this.clientIdentity.IsLocalPlayer, Is.False);
        });

        [UnityTest]
        public IEnumerator RemoveCharacter() => UniTask.ToCoroutine(async () =>
        {
            this.serverObjectManager.RemoveCharacter(this.serverPlayer);

            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(this.serverPlayerGO != null);
            Assert.That(this.serverIdentity.Owner == null);

            Assert.That(this.clientPlayerGO != null);
            Assert.That(this.clientIdentity.HasAuthority, Is.False);
            Assert.That(this.clientIdentity.IsLocalPlayer, Is.False);
        });

        [Test]
        public void DestroyCharacterThrowsIfNoCharacter()
        {
            var player = Substitute.For<INetworkPlayer>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.DestroyCharacter(player);
            });
        }

        [Test]
        public void RemoveCharacterThrowsIfNoCharacter()
        {
            var player = Substitute.For<INetworkPlayer>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.RemoveCharacter(player, false);
            });
        }

        [Test]
        public void ThrowsIfSpawnedCalledWithoutANetworkIdentity()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.Spawn(new GameObject(), this.clientPlayer);
            });

            this.AssertNoIdentityMessage(ex);
        }


        [Test]
        public void AddCharacterNoIdentityExceptionTest()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.AddCharacter(this.serverPlayer, new GameObject());
            });
            this.AssertNoIdentityMessage(ex);

        }

        [Test]
        public void ReplacePlayerNoIdentityExceptionTest()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.ReplaceCharacter(this.serverPlayer, new GameObject(), true);
            });
            this.AssertNoIdentityMessage(ex);
        }

        [UnityTest]
        public IEnumerator SpawnObjectsExceptionTest() => UniTask.ToCoroutine(async () =>
        {
            this.server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active);

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverObjectManager.SpawnObjects();
            });

            Assert.That(exception, Has.Message.EqualTo("Server was not active"));
        });
    }
}


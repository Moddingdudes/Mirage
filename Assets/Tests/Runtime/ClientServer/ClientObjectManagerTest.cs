using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.ClientServer
{
    [TestFixture]
    public class ClientObjectManagerTest : ClientServerSetup<MockComponent>
    {
        private GameObject playerReplacement;

        [Test]
        public void OnSpawnAssetSceneIDFailureExceptionTest()
        {
            var msg = new SpawnMessage();
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                this.clientObjectManager.OnSpawn(msg);
            });

            Assert.That(ex.Message, Is.EqualTo($"OnSpawn has empty prefabHash and sceneId for netId: {msg.netId}"));
        }

        [UnityTest]
        public IEnumerator GetPrefabTest() => UniTask.ToCoroutine(async () =>
        {
            var hash = this.NewUniqueHash();
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));
            var identity = prefabObject.GetComponent<NetworkIdentity>();

            this.clientObjectManager.RegisterPrefab(identity, hash);

            await UniTask.Delay(1);

            var result = this.clientObjectManager.GetPrefab(hash);

            Assert.That(result, Is.SameAs(identity));

            Object.Destroy(prefabObject);
        });

        [Test]
        public void RegisterPrefabDelegateEmptyIdentityExceptionTest()
        {
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));
            var identity = prefabObject.GetComponent<NetworkIdentity>();
            identity.PrefabHash = 0;

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.clientObjectManager.RegisterPrefab(identity, this.TestSpawnDelegate, this.TestUnspawnDelegate);
            });

            Object.Destroy(prefabObject);
        }

        [Test]
        public void RegisterPrefabDelegateTest()
        {
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));
            var identity = prefabObject.GetComponent<NetworkIdentity>();
            identity.PrefabHash = this.NewUniqueHash();

            this.clientObjectManager.RegisterPrefab(identity, this.TestSpawnDelegate, this.TestUnspawnDelegate);

            Assert.That(this.clientObjectManager.spawnHandlers.ContainsKey(identity.PrefabHash));
            Assert.That(this.clientObjectManager.unspawnHandlers.ContainsKey(identity.PrefabHash));

            Object.Destroy(prefabObject);
        }

        [Test]
        public void UnregisterPrefabTest()
        {
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));
            var identity = prefabObject.GetComponent<NetworkIdentity>();
            identity.PrefabHash = this.NewUniqueHash();

            this.clientObjectManager.RegisterPrefab(identity, this.TestSpawnDelegate, this.TestUnspawnDelegate);

            Assert.That(this.clientObjectManager.spawnHandlers.ContainsKey(identity.PrefabHash));
            Assert.That(this.clientObjectManager.unspawnHandlers.ContainsKey(identity.PrefabHash));

            this.clientObjectManager.UnregisterPrefab(identity);

            Assert.That(!this.clientObjectManager.spawnHandlers.ContainsKey(identity.PrefabHash));
            Assert.That(!this.clientObjectManager.unspawnHandlers.ContainsKey(identity.PrefabHash));

            Object.Destroy(prefabObject);
        }

        [Test]
        public void UnregisterSpawnHandlerTest()
        {
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));
            var identity = prefabObject.GetComponent<NetworkIdentity>();
            identity.PrefabHash = this.NewUniqueHash();

            this.clientObjectManager.RegisterPrefab(identity, this.TestSpawnDelegate, this.TestUnspawnDelegate);

            Assert.That(this.clientObjectManager.spawnHandlers.ContainsKey(identity.PrefabHash));
            Assert.That(this.clientObjectManager.unspawnHandlers.ContainsKey(identity.PrefabHash));

            this.clientObjectManager.UnregisterSpawnHandler(identity.PrefabHash);

            Assert.That(!this.clientObjectManager.spawnHandlers.ContainsKey(identity.PrefabHash));
            Assert.That(!this.clientObjectManager.unspawnHandlers.ContainsKey(identity.PrefabHash));

            Object.Destroy(prefabObject);
        }

        private NetworkIdentity TestSpawnDelegate(SpawnMessage msg)
        {
            return new GameObject("spawned", typeof(NetworkIdentity)).GetComponent<NetworkIdentity>();
        }

        private void TestUnspawnDelegate(NetworkIdentity identity)
        {
            Object.Destroy(identity.gameObject);
        }

        [Test]
        public void GetPrefabEmptyNullTest()
        {
            var result = this.clientObjectManager.GetPrefab(0);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetPrefabNotFoundNullTest()
        {
            var result = this.clientObjectManager.GetPrefab(this.NewUniqueHash());

            Assert.That(result, Is.Null);
        }

        //Used to ensure the test has a unique non empty guid
        private int NewUniqueHash()
        {
            var testGuid = Guid.NewGuid().GetHashCode();

            if (this.clientObjectManager.prefabs.ContainsKey(testGuid))
            {
                testGuid = this.NewUniqueHash();
            }
            return testGuid;
        }

        [UnityTest]
        public IEnumerator ObjectHideTest() => UniTask.ToCoroutine(async () =>
        {
            this.clientObjectManager.OnObjectHide(new ObjectHideMessage
            {
                netId = this.clientIdentity.NetId
            });

            await AsyncUtil.WaitUntilWithTimeout(() => this.clientIdentity == null);

            Assert.That(this.clientIdentity == null);
        });

        [UnityTest]
        public IEnumerator ObjectDestroyTest() => UniTask.ToCoroutine(async () =>
        {
            this.clientObjectManager.OnObjectDestroy(new ObjectDestroyMessage
            {
                netId = this.clientIdentity.NetId
            });

            await AsyncUtil.WaitUntilWithTimeout(() => this.clientIdentity == null);

            Assert.That(this.clientIdentity == null);
        });

        [Test]
        public void SpawnSceneObjectTest()
        {
            //Setup new scene object for test
            var hash = this.NewUniqueHash();
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));
            var identity = prefabObject.GetComponent<NetworkIdentity>();
            identity.PrefabHash = hash;
            var sceneId = 10ul;
            this.clientObjectManager.spawnableObjects.Add(sceneId, identity);

            var result = this.clientObjectManager.SpawnSceneObject(new SpawnMessage { sceneId = sceneId, prefabHash = hash });

            Assert.That(result, Is.SameAs(identity));

            Object.Destroy(prefabObject);
        }
    }
}

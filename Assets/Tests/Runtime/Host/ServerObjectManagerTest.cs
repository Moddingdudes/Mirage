using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class ServerObjectManagerHostTest : HostSetup<MockComponent>
    {
        [Test]
        public void HideForPlayerTest()
        {
            // add connection

            var player = Substitute.For<INetworkPlayer>();

            var identity = new GameObject().AddComponent<NetworkIdentity>();

            this.serverObjectManager.HideToPlayer(identity, player);

            player.Received().Send(Arg.Is<ObjectHideMessage>(msg => msg.netId == identity.NetId));

            // destroy GO after shutdown, otherwise isServer is true in OnDestroy and it tries to call
            // GameObject.Destroy (but we need DestroyImmediate in Editor)
            Object.Destroy(identity.gameObject);
        }

        [Test]
        public void ValidateSceneObject()
        {
            this.identity.SetSceneId(42);
            Assert.That(this.serverObjectManager.ValidateSceneObject(this.identity), Is.True);
            this.identity.SetSceneId(0);
            Assert.That(this.serverObjectManager.ValidateSceneObject(this.identity), Is.False);
        }

        [Test]
        public void HideFlagsTest()
        {
            // shouldn't be valid for certain hide flags
            this.playerGO.hideFlags = HideFlags.NotEditable;
            Assert.That(this.serverObjectManager.ValidateSceneObject(this.identity), Is.False);
            this.playerGO.hideFlags = HideFlags.HideAndDontSave;
            Assert.That(this.serverObjectManager.ValidateSceneObject(this.identity), Is.False);
        }

        [Test]
        public void UnSpawn()
        {
            // unspawn
            this.serverObjectManager.Destroy(this.playerGO, false);

            // it should have been marked for reset now
            Assert.That(this.identity.NetId, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DestroyAllSpawnedOnStopTest() => UniTask.ToCoroutine(async () =>
        {
            var spawnTestObj = new GameObject("testObj", typeof(NetworkIdentity));
            this.serverObjectManager.Spawn(spawnTestObj);

            // need to grab reference to world before Stop, becuase stop will clear reference
            var world = this.server.World;

            //1 is the player. should be 2 at this point
            Assert.That(world.SpawnedIdentities.Count, Is.GreaterThan(1));


            this.server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active);

            Assert.That(world.SpawnedIdentities.Count, Is.Zero);
            // checks that the object was destroyed
            // use unity null check here
            Assert.IsTrue(spawnTestObj == null);
        });
    }
}

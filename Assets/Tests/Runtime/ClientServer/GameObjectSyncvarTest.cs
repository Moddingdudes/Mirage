using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class SampleBehaviorWithGO : NetworkBehaviour
    {
        [SyncVar]
        public GameObject target;
    }

    public class GameObjectSyncvarTest : ClientServerSetup<SampleBehaviorWithGO>
    {
        [Test]
        public void IsNullByDefault()
        {
            // out of the box, target should be null in the client

            Assert.That(this.clientComponent.target, Is.Null);
        }

        [UnityTest]
        public IEnumerator ChangeTarget() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.target = this.serverPlayerGO;

            await AsyncUtil.WaitUntilWithTimeout(() => this.clientComponent.target != null);

            Assert.That(this.clientComponent.target, Is.SameAs(this.clientPlayerGO));
        });

        [Test]
        public void UpdateAfterSpawn()
        {
            // this situation can happen when the client does nto see an object
            // but the object is assigned in a syncvar.
            // this can easily happen during spawn if spawning in an unexpected order
            // or if there is AOI in play.
            // in this case we would have a valid net id, but we would not
            // find the object at spawn time

            var goSyncvar = new GameObjectSyncvar
            {
                objectLocator = this.client.World,
                netId = this.serverIdentity.NetId,
                gameObject = null,
            };

            Assert.That(goSyncvar.Value, Is.SameAs(this.clientPlayerGO));
        }

        [UnityTest]
        public IEnumerator SpawnWithTarget() => UniTask.ToCoroutine(async () =>
        {
            // create an object, set the target and spawn it
            var newObject = Object.Instantiate(this.playerPrefab);
            var newBehavior = newObject.GetComponent<SampleBehaviorWithGO>();
            newBehavior.target = this.serverPlayerGO;
            this.serverObjectManager.Spawn(newObject);

            // wait until the client spawns it
            var newObjectId = newBehavior.NetId;

            var newClientObject = await AsyncUtil.WaitUntilSpawn(this.client.World, newObjectId);

            // check if the target was set correctly in the client
            var newClientBehavior = newClientObject.GetComponent<SampleBehaviorWithGO>();
            Assert.That(newClientBehavior.target, Is.SameAs(this.clientPlayerGO));

            // cleanup
            this.serverObjectManager.Destroy(newObject);
        });
    }
}

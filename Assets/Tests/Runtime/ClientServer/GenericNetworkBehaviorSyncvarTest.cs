using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class GenericBehaviourWithSyncVar<T> : NetworkBehaviour
    {
        [SyncVar]
        public int baseValue = 0;
        [SyncVar(hook = nameof(OnSyncedBaseValueWithHook))]
        public int baseValueWithHook = 0;
        [SyncVar]
        public NetworkBehaviour target;
        [SyncVar]
        public NetworkIdentity targetIdentity;

        public Action<int, int> onBaseValueChanged;

        // Not used, just here to trigger possible errors.
        private T value;
        private T[] values;

        public void OnSyncedBaseValueWithHook(int oldValue, int newValue)
        {
            this.onBaseValueChanged?.Invoke(oldValue, newValue);
        }
    }

    public class GenericBehaviourWithSyncVarImplement : GenericBehaviourWithSyncVar<UnityEngine.Vector3>
    {
        [SyncVar]
        public int childValue = 0;
        [SyncVar(hook = nameof(OnSyncedChildValueWithHook))]
        public int childValueWithHook = 0;
        [SyncVar]
        public NetworkBehaviour childTarget;
        [SyncVar]
        public NetworkIdentity childIdentity;

        public Action<int, int> onChildValueChanged;

        public void OnSyncedChildValueWithHook(int oldValue, int newValue)
        {
            this.onChildValueChanged?.Invoke(oldValue, newValue);
        }
    }

    public class GenericNetworkBehaviorSyncvarTest : ClientServerSetup<GenericBehaviourWithSyncVarImplement>
    {
        [Test]
        public void IsZeroByDefault()
        {
            Assert.AreEqual(this.clientComponent.baseValue, 0);
            Assert.AreEqual(this.clientComponent.baseValueWithHook, 0);
            Assert.AreEqual(this.clientComponent.childValue, 0);
            Assert.AreEqual(this.clientComponent.childValueWithHook, 0);
            Assert.IsNull(this.clientComponent.target);
            Assert.IsNull(this.clientComponent.targetIdentity);
            Assert.IsNull(this.clientComponent.childTarget);
            Assert.IsNull(this.clientComponent.childIdentity);
        }

        [UnityTest]
        public IEnumerator ChangeValue() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.baseValue = 2;

            await UniTask.WaitUntil(() => this.clientComponent.baseValue != 0);

            Assert.AreEqual(this.clientComponent.baseValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeValueHook() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.baseValueWithHook = 2;
            this.clientComponent.onBaseValueChanged += (oldValue, newValue) =>
            {
                Assert.AreEqual(0, oldValue);
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => this.clientComponent.baseValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeTarget() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.target = this.serverComponent;

            await UniTask.WaitUntil(() => this.clientComponent.target != null);

            Assert.That(this.clientComponent.target, Is.SameAs(this.clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.targetIdentity = this.serverIdentity;

            await UniTask.WaitUntil(() => this.clientComponent.targetIdentity != null);

            Assert.That(this.clientComponent.targetIdentity, Is.SameAs(this.clientIdentity));
        });

        [UnityTest]
        public IEnumerator ChangeChildValue() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.childValue = 2;

            await UniTask.WaitUntil(() => this.clientComponent.childValue != 0);

            Assert.AreEqual(this.clientComponent.childValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeChildValueHook() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.childValueWithHook = 2;
            this.clientComponent.onChildValueChanged += (oldValue, newValue) =>
            {
                Assert.AreEqual(0, oldValue);
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => this.clientComponent.childValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeChildTarget() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.childTarget = this.serverComponent;

            await UniTask.WaitUntil(() => this.clientComponent.childTarget != null);

            Assert.That(this.clientComponent.childTarget, Is.SameAs(this.clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeChildNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.childIdentity = this.serverIdentity;

            await UniTask.WaitUntil(() => this.clientComponent.childIdentity != null);

            Assert.That(this.clientComponent.childIdentity, Is.SameAs(this.clientIdentity));
        });

        [UnityTest]
        public IEnumerator SpawnWithValue() => UniTask.ToCoroutine(async () =>
        {
            // create an object, set the target and spawn it
            var newObject = UnityEngine.Object.Instantiate(this.playerPrefab);
            var newBehavior = newObject.GetComponent<GenericBehaviourWithSyncVarImplement>();
            newBehavior.baseValue = 2;
            newBehavior.childValue = 22;
            newBehavior.target = this.serverComponent;
            newBehavior.targetIdentity = this.serverIdentity;
            newBehavior.childTarget = this.serverComponent;
            newBehavior.childIdentity = this.serverIdentity;
            this.serverObjectManager.Spawn(newObject);

            // wait until the client spawns it
            var newObjectId = newBehavior.NetId;

            var newClientObject = await AsyncUtil.WaitUntilSpawn(this.client.World, newObjectId);
            // check if the target was set correctly in the client

            var newClientBehavior = newClientObject.GetComponent<GenericBehaviourWithSyncVarImplement>();
            Assert.AreEqual(newClientBehavior.baseValue, 2);
            Assert.AreEqual(newClientBehavior.childValue, 22);
            Assert.That(newClientBehavior.target, Is.SameAs(this.clientComponent));
            Assert.That(newClientBehavior.targetIdentity, Is.SameAs(this.clientIdentity));
            Assert.That(newClientBehavior.childTarget, Is.SameAs(this.clientComponent));
            Assert.That(newClientBehavior.childIdentity, Is.SameAs(this.clientIdentity));

            // cleanup
            this.serverObjectManager.Destroy(newObject);
        });
    }
}

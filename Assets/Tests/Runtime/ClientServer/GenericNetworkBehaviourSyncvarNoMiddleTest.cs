using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class GenericBehaviourWithSyncVarNoMiddleBase<T> : NetworkBehaviour
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

    public class GenericBehaviourWithSyncVarNoMiddleMiddle<T> : GenericBehaviourWithSyncVarNoMiddleBase<T>
    {
        // Not used, just here to trigger possible errors.
        private T middleValue;
        private T[] middleValues;
    }

    public class GenericBehaviourWithSyncVarNoMiddleImplement : GenericBehaviourWithSyncVarNoMiddleMiddle<UnityEngine.Vector3>
    {
        [SyncVar]
        public int implementValue = 0;
        [SyncVar(hook = nameof(OnSyncedImplementValueWithHook))]
        public int implementValueWithHook = 0;
        [SyncVar]
        public NetworkBehaviour implementTarget;
        [SyncVar]
        public NetworkIdentity implementIdentity;

        public Action<int, int> onImplementValueChanged;

        public void OnSyncedImplementValueWithHook(int oldValue, int newValue)
        {
            this.onImplementValueChanged?.Invoke(oldValue, newValue);
        }
    }

    public class GenericNetworkBehaviorSyncvarNoMiddleTest : ClientServerSetup<GenericBehaviourWithSyncVarNoMiddleImplement>
    {
        [Test]
        public void IsZeroByDefault()
        {
            Assert.AreEqual(this.clientComponent.baseValue, 0);
            Assert.AreEqual(this.clientComponent.baseValueWithHook, 0);
            Assert.AreEqual(this.clientComponent.implementValue, 0);
            Assert.AreEqual(this.clientComponent.implementValueWithHook, 0);
            Assert.IsNull(this.clientComponent.target);
            Assert.IsNull(this.clientComponent.targetIdentity);
            Assert.IsNull(this.clientComponent.implementTarget);
            Assert.IsNull(this.clientComponent.implementIdentity);
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
        public IEnumerator ChangeImplementValue() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.implementValue = 2;

            await UniTask.WaitUntil(() => this.clientComponent.implementValue != 0);

            Assert.AreEqual(this.clientComponent.implementValue, 2);
        });

        [UnityTest]
        public IEnumerator ChangeImplementValueHook() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.implementValueWithHook = 2;
            this.clientComponent.onImplementValueChanged += (oldValue, newValue) =>
            {
                Assert.AreEqual(0, oldValue);
                Assert.AreEqual(2, newValue);
            };

            await UniTask.WaitUntil(() => this.clientComponent.implementValueWithHook != 0);
        });

        [UnityTest]
        public IEnumerator ChangeImplementTarget() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.implementTarget = this.serverComponent;

            await UniTask.WaitUntil(() => this.clientComponent.implementTarget != null);

            Assert.That(this.clientComponent.implementTarget, Is.SameAs(this.clientComponent));
        });

        [UnityTest]
        public IEnumerator ChangeImplementNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.implementIdentity = this.serverIdentity;

            await UniTask.WaitUntil(() => this.clientComponent.implementIdentity != null);

            Assert.That(this.clientComponent.implementIdentity, Is.SameAs(this.clientIdentity));
        });

        [UnityTest]
        public IEnumerator SpawnWithValue() => UniTask.ToCoroutine(async () =>
        {
            // create an object, set the target and spawn it
            var newObject = UnityEngine.Object.Instantiate(this.playerPrefab);
            var newBehavior = newObject.GetComponent<GenericBehaviourWithSyncVarNoMiddleImplement>();
            newBehavior.baseValue = 2;
            newBehavior.implementValue = 222;
            newBehavior.target = this.serverComponent;
            newBehavior.targetIdentity = this.serverIdentity;
            newBehavior.implementTarget = this.serverComponent;
            newBehavior.implementIdentity = this.serverIdentity;
            this.serverObjectManager.Spawn(newObject);

            // wait until the client spawns it
            var newObjectId = newBehavior.NetId;
            var newClientObject = await AsyncUtil.WaitUntilSpawn(this.client.World, newObjectId);

            // check if the target was set correctly in the client
            var newClientBehavior = newClientObject.GetComponent<GenericBehaviourWithSyncVarNoMiddleImplement>();
            Assert.AreEqual(newClientBehavior.baseValue, 2);
            Assert.AreEqual(newClientBehavior.implementValue, 222);
            Assert.That(newClientBehavior.target, Is.SameAs(this.clientComponent));
            Assert.That(newClientBehavior.targetIdentity, Is.SameAs(this.clientIdentity));
            Assert.That(newClientBehavior.implementTarget, Is.SameAs(this.clientComponent));
            Assert.That(newClientBehavior.implementIdentity, Is.SameAs(this.clientIdentity));

            // cleanup
            this.serverObjectManager.Destroy(newObject);
        });
    }
}

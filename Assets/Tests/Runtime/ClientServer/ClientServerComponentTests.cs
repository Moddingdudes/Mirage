using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Guid = System.Guid;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class ClientServerComponentTests : ClientServerSetup<MockComponent>
    {
        [Test]
        public void CheckNotHost()
        {
            Assert.That(this.serverPlayerGO, Is.Not.SameAs(this.clientPlayerGO));

            Assert.That(this.serverPlayerGO, Is.Not.Null);
            Assert.That(this.clientPlayerGO, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ServerRpc() => UniTask.ToCoroutine(async () =>
        {
            this.clientComponent.Send2Args(1, "hello");

            await AsyncUtil.WaitUntilWithTimeout(() => this.serverComponent.cmdArg1 != 0);

            Assert.That(this.serverComponent.cmdArg1, Is.EqualTo(1));
            Assert.That(this.serverComponent.cmdArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator ServerRpcWithSenderOnClient() => UniTask.ToCoroutine(async () =>
        {
            this.clientComponent.SendWithSender(1);

            await AsyncUtil.WaitUntilWithTimeout(() => this.serverComponent.cmdArg1 != 0);

            Assert.That(this.serverComponent.cmdArg1, Is.EqualTo(1));
            Assert.That(this.serverComponent.cmdSender, Is.EqualTo(this.serverPlayer), "ServerRpc called on client will have client's player (server version)");
        });

        [UnityTest]
        public IEnumerator ServerRpcWithSenderOnServer() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.SendWithSender(1);

            await AsyncUtil.WaitUntilWithTimeout(() => this.serverComponent.cmdArg1 != 0);

            Assert.That(this.serverComponent.cmdArg1, Is.EqualTo(1));
            Assert.That(this.serverComponent.cmdSender, Is.Null, "ServerRPC called on server will have no sender");
        });

        [UnityTest]
        public IEnumerator ServerRpcWithNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            this.clientComponent.CmdNetworkIdentity(this.clientIdentity);

            await AsyncUtil.WaitUntilWithTimeout(() => this.serverComponent.cmdNi != null);

            Assert.That(this.serverComponent.cmdNi, Is.SameAs(this.serverIdentity));
        });

        [UnityTest]
        public IEnumerator ClientRpc() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.RpcTest(1, "hello");
            // process spawn message from server
            await AsyncUtil.WaitUntilWithTimeout(() => this.clientComponent.rpcArg1 != 0);

            Assert.That(this.clientComponent.rpcArg1, Is.EqualTo(1));
            Assert.That(this.clientComponent.rpcArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator ClientConnRpc() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.ClientConnRpcTest(this.serverPlayer, 1, "hello");
            // process spawn message from server
            await AsyncUtil.WaitUntilWithTimeout(() => this.clientComponent.targetRpcArg1 != 0);

            Assert.That(this.clientComponent.targetRpcPlayer, Is.EqualTo(this.clientPlayer));
            Assert.That(this.clientComponent.targetRpcArg1, Is.EqualTo(1));
            Assert.That(this.clientComponent.targetRpcArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator ClientOwnerRpc() => UniTask.ToCoroutine(async () =>
        {
            this.serverComponent.RpcOwnerTest(1, "hello");
            // process spawn message from server
            await AsyncUtil.WaitUntilWithTimeout(() => this.clientComponent.rpcOwnerArg1 != 0);

            Assert.That(this.clientComponent.rpcOwnerArg1, Is.EqualTo(1));
            Assert.That(this.clientComponent.rpcOwnerArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator OnSpawnSpawnHandlerTest() => UniTask.ToCoroutine(async () =>
        {
            this.spawnDelegateTestCalled = 0;
            var hash = Guid.NewGuid().GetHashCode();
            var gameObject = new GameObject();
            var identity = gameObject.AddComponent<NetworkIdentity>();
            identity.PrefabHash = hash;
            identity.NetId = (uint)Random.Range(0, int.MaxValue);

            this.clientObjectManager.RegisterSpawnHandler(hash, this.SpawnDelegateTest, go => { });
            this.clientObjectManager.RegisterPrefab(identity, hash);
            this.serverObjectManager.SendSpawnMessage(identity, this.serverPlayer);

            await AsyncUtil.WaitUntilWithTimeout(() => this.spawnDelegateTestCalled != 0);

            Assert.That(this.spawnDelegateTestCalled, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator OnDestroySpawnHandlerTest() => UniTask.ToCoroutine(async () =>
        {
            this.spawnDelegateTestCalled = 0;
            var hash = Guid.NewGuid().GetHashCode();
            var gameObject = new GameObject();
            var identity = gameObject.AddComponent<NetworkIdentity>();
            identity.PrefabHash = hash;
            identity.NetId = (uint)Random.Range(0, int.MaxValue);

            var unspawnDelegate = Substitute.For<UnSpawnDelegate>();

            this.clientObjectManager.RegisterSpawnHandler(hash, this.SpawnDelegateTest, unspawnDelegate);
            this.clientObjectManager.RegisterPrefab(identity, hash);
            this.serverObjectManager.SendSpawnMessage(identity, this.serverPlayer);

            await AsyncUtil.WaitUntilWithTimeout(() => this.spawnDelegateTestCalled != 0);

            this.clientObjectManager.OnObjectDestroy(new ObjectDestroyMessage
            {
                netId = identity.NetId
            });
            unspawnDelegate.Received().Invoke(Arg.Any<NetworkIdentity>());
        });

        private int spawnDelegateTestCalled;

        private NetworkIdentity SpawnDelegateTest(SpawnMessage msg)
        {
            this.spawnDelegateTestCalled++;

            var prefab = this.clientObjectManager.GetPrefab(msg.prefabHash.Value);
            if (!(prefab is null))
            {
                return Object.Instantiate(prefab);
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ClientDisconnectTest() => UniTask.ToCoroutine(async () =>
        {
            var playerCount = this.server.Players.Count;
            this.client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => this.client.connectState == ConnectState.Disconnected);
            // player could should be 1 less after client disconnects
            await AsyncUtil.WaitUntilWithTimeout(() => this.server.Players.Count == playerCount - 1);
        });
    }
}

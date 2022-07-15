using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    public class HostComponentTests : HostSetup<MockComponent>
    {
        [UnityTest]
        public IEnumerator ServerRpc() => UniTask.ToCoroutine(async () =>
        {
            this.component.Send2Args(1, "hello");

            await AsyncUtil.WaitUntilWithTimeout(() => this.component.cmdArg1 != 0);

            Assert.That(this.component.cmdArg1, Is.EqualTo(1));
            Assert.That(this.component.cmdArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator ServerRpcWithSender() => UniTask.ToCoroutine(async () =>
        {
            this.component.SendWithSender(1);

            await AsyncUtil.WaitUntilWithTimeout(() => this.component.cmdArg1 != 0);

            Assert.That(this.component.cmdArg1, Is.EqualTo(1));
            Assert.That(this.component.cmdSender, Is.EqualTo(this.server.LocalPlayer), "Server Rpc call on host will have localplayer (server version) as sender");
        });

        [UnityTest]
        public IEnumerator ServerRpcWithNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            this.component.CmdNetworkIdentity(this.identity);

            await AsyncUtil.WaitUntilWithTimeout(() => this.component.cmdNi != null);

            Assert.That(this.component.cmdNi, Is.SameAs(this.identity));
        });

        [UnityTest]
        public IEnumerator ClientRpc() => UniTask.ToCoroutine(async () =>
        {
            this.component.RpcTest(1, "hello");
            // process spawn message from server
            await AsyncUtil.WaitUntilWithTimeout(() => this.component.rpcArg1 != 0);

            Assert.That(this.component.rpcArg1, Is.EqualTo(1));
            Assert.That(this.component.rpcArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator ClientConnRpc() => UniTask.ToCoroutine(async () =>
        {
            this.component.ClientConnRpcTest(this.manager.Server.LocalPlayer, 1, "hello");
            // process spawn message from server
            await AsyncUtil.WaitUntilWithTimeout(() => this.component.targetRpcArg1 != 0);

            Assert.That(this.component.targetRpcPlayer, Is.EqualTo(this.manager.Client.Player));
            Assert.That(this.component.targetRpcArg1, Is.EqualTo(1));
            Assert.That(this.component.targetRpcArg2, Is.EqualTo("hello"));
        });

        [UnityTest]
        public IEnumerator ClientOwnerRpc() => UniTask.ToCoroutine(async () =>
        {
            this.component.RpcOwnerTest(1, "hello");
            // process spawn message from server
            await AsyncUtil.WaitUntilWithTimeout(() => this.component.rpcOwnerArg1 != 0);

            Assert.That(this.component.rpcOwnerArg1, Is.EqualTo(1));
            Assert.That(this.component.rpcOwnerArg2, Is.EqualTo("hello"));
        });

        [Test]
        public void StopHostTest()
        {
            this.server.Stop();

            // state cleared?
            Assert.That(this.server.Players, Is.Empty);
            Assert.That(this.server.Active, Is.False);
            Assert.That(this.server.LocalPlayer, Is.Null);
            Assert.That(this.server.LocalClientActive, Is.False);
        }

        [Test]
        public void StoppingHostShouldCallDisconnectedOnLocalClient()
        {
            var invoked = 0;
            this.client.Disconnected.AddListener((reason) =>
            {
                Assert.That(reason, Is.EqualTo(ClientStoppedReason.HostModeStopped));
                invoked++;
            });

            this.server.Stop();

            // state cleared?
            Assert.That(invoked, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ClientSceneChangedOnReconnect() => UniTask.ToCoroutine(async () =>
        {
            this.server.Stop();

            // wait for server to disconnect
            await UniTask.WaitUntil(() => !this.server.Active);

            var mockListener = Substitute.For<UnityAction<string, SceneOperation>>();
            this.sceneManager.OnClientStartedSceneChange.AddListener(mockListener);
            await this.StartHost();

            this.client.Update();
            mockListener.Received().Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
        });
    }
}

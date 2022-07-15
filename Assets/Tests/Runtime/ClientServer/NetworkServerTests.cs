using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class NetworkServerTests : ClientServerSetup<MockComponent>
    {
        private WovenTestMessage message;

        public override void ExtraSetup()
        {
            this.message = new WovenTestMessage
            {
                IntValue = 1,
                DoubleValue = 1.0,
                StringValue = "hello"
            };

        }

        [Test]
        public void InitializeTest()
        {
            Assert.That(this.server.Players, Has.Count.EqualTo(1));
            Assert.That(this.server.Active);
            Assert.That(this.server.LocalClientActive, Is.False);
        }

        [Test]
        public void ThrowsIfListenIsCalledWhileAlreadyActive()
        {
            var expection = Assert.Throws<InvalidOperationException>(() =>
            {
                this.server.StartServer();
            });
            Assert.That(expection, Has.Message.EqualTo("Server is already active"));
        }

        [UnityTest]
        public IEnumerator ReadyMessageSetsClientReadyTest() => UniTask.ToCoroutine(async () =>
        {
            this.clientPlayer.Send(new SceneReadyMessage());

            await AsyncUtil.WaitUntilWithTimeout(() => this.serverPlayer.SceneIsReady);

            // ready?
            Assert.That(this.serverPlayer.SceneIsReady, Is.True);
        });

        [UnityTest]
        public IEnumerator SendToAll() => UniTask.ToCoroutine(async () =>
        {
            var invoked = false;

            this.ClientMessageHandler.RegisterHandler<WovenTestMessage>(msg => invoked = true);

            this.server.SendToAll(this.message);

            // todo assert correct message was sent using Substitute for socket or player
            // connectionToServer.ProcessMessagesAsync().Forget();

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [UnityTest]
        public IEnumerator SendToClientOfPlayer() => UniTask.ToCoroutine(async () =>
        {
            var invoked = false;

            this.ClientMessageHandler.RegisterHandler<WovenTestMessage>(msg => invoked = true);

            this.serverIdentity.Owner.Send(this.message);

            // todo assert correct message was sent using Substitute for socket or player
            // connectionToServer.ProcessMessagesAsync().Forget();

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [UnityTest]
        public IEnumerator RegisterMessage1() => UniTask.ToCoroutine(async () =>
        {
            var invoked = false;

            this.ServerMessageHandler.RegisterHandler<WovenTestMessage>(msg => invoked = true);
            this.clientPlayer.Send(this.message);

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);

        });

        [UnityTest]
        public IEnumerator RegisterMessage2() => UniTask.ToCoroutine(async () =>
        {
            var invoked = false;

            this.ServerMessageHandler.RegisterHandler<WovenTestMessage>((conn, msg) => invoked = true);

            this.clientPlayer.Send(this.message);

            await AsyncUtil.WaitUntilWithTimeout(() => invoked);
        });

        [UnityTest]
        public IEnumerator UnRegisterMessage1() => UniTask.ToCoroutine(async () =>
        {
            var func = Substitute.For<MessageDelegate<WovenTestMessage>>();

            this.ServerMessageHandler.RegisterHandler(func);
            this.ServerMessageHandler.UnregisterHandler<WovenTestMessage>();

            this.clientPlayer.Send(this.message);

            await UniTask.Delay(1);

            func.Received(0).Invoke(
                Arg.Any<WovenTestMessage>());
        });

        [Test]
        public void NumPlayersTest()
        {
            Assert.That(this.server.NumberOfPlayers, Is.EqualTo(1));
        }

        [Test]
        public void VariableTest()
        {
            Assert.That(this.server.MaxConnections, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator StopStateTest() => UniTask.ToCoroutine(async () =>
        {
            this.server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active);
        });

        [UnityTest]
        public IEnumerator StoppedInvokeTest() => UniTask.ToCoroutine(async () =>

        {
            var func1 = Substitute.For<UnityAction>();
            this.server.Stopped.AddListener(func1);

            this.server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active);

            func1.Received(1).Invoke();
        });

        public IEnumerator ApplicationQuitTest() => UniTask.ToCoroutine(async () =>
        {
            var func1 = Substitute.For<UnityAction>();
            this.server.Stopped.AddListener(func1);

            await UniTask.Delay(1);

            Application.Quit();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active);

            func1.Received(1).Invoke();
        });

        [UnityTest]
        public IEnumerator DisconnectCalledBeforePlayerIsDestroyed()
        {
            var serverPlayer = base.serverPlayer;
            var disconnectCalled = 0;
            this.server.Disconnected.AddListener(player =>
            {
                disconnectCalled++;
                Assert.That(player, Is.EqualTo(serverPlayer));
                // use unity null check
                Assert.That(player.HasCharacter);
            });


            this.client.Disconnect();
            // wait a tick for messages to be processed
            yield return null;

            Assert.That(disconnectCalled, Is.EqualTo(1));
            Assert.That(serverPlayer.HasCharacter, Is.False);

        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Mirage.Tests.Runtime.Host
{
    public class NetworkSceneManagerTests : HostSetup<MockComponent>
    {
        private AssetBundle bundle;
        private UnityAction<Scene, SceneOperation> sceneEventFunction;

        public override void ExtraSetup()
        {
            this.bundle = AssetBundle.LoadFromFile("Assets/Tests/Runtime/TestScene/testscene");

            this.sceneEventFunction = Substitute.For<UnityAction<Scene, SceneOperation>>();
            this.sceneManager.OnServerFinishedSceneChange.AddListener(this.sceneEventFunction);
        }

        public override void ExtraTearDown()
        {
            this.bundle.Unload(true);
        }

        [Test]
        public void FinishLoadSceneHostTest()
        {
            var func2 = Substitute.For<UnityAction<Scene, SceneOperation>>();
            var func3 = Substitute.For<UnityAction<Scene, SceneOperation>>();

            this.sceneManager.OnServerFinishedSceneChange.AddListener(func2);
            this.sceneManager.OnClientFinishedSceneChange.AddListener(func3);

            this.sceneManager.CompleteLoadingScene(default, SceneOperation.Normal);

            func2.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
            func3.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
        }

        [UnityTest]
        public IEnumerator FinishLoadServerOnlyTest() => UniTask.ToCoroutine(async () =>
        {
            var func1 = Substitute.For<UnityAction<Scene, SceneOperation>>();

            this.client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.Active);

            this.sceneManager.OnServerFinishedSceneChange.AddListener(func1);

            this.sceneManager.CompleteLoadingScene(default, SceneOperation.Normal);

            func1.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
        });

        [UnityTest]
        public IEnumerator ServerChangeSceneTest() => UniTask.ToCoroutine(async () =>
        {
            var invokeClientSceneMessage = false;
            var invokeNotReadyMessage = false;
            var func1 = Substitute.For<UnityAction<string, SceneOperation>>();
            this.ClientMessageHandler.RegisterHandler<SceneMessage>(msg => invokeClientSceneMessage = true);
            this.ClientMessageHandler.RegisterHandler<SceneNotReadyMessage>(msg => invokeNotReadyMessage = true);
            this.sceneManager.OnServerStartedSceneChange.AddListener(func1);

            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            await AsyncUtil.WaitUntilWithTimeout(() => this.sceneManager.ActiveScenePath != null);

            func1.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
            Assert.That(this.sceneManager.ActiveScenePath, Is.Not.Null);
            Assert.That(invokeClientSceneMessage, Is.True);
            Assert.That(invokeNotReadyMessage, Is.True);
        });

        [Test]
        public void ServerChangedFiredOnceTest()
        {
            this.sceneEventFunction.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void CheckServerSceneDataNotEmptyTest()
        {
            Assert.That(this.sceneManager.ServerSceneData, Is.Not.Null);
        }

        [Test]
        public void ChangeServerSceneExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                this.sceneManager.ServerLoadSceneNormal(string.Empty);
            });
        }

        [Test]
        public void ReadyTest()
        {
            this.sceneManager.SetSceneIsReady();
            Assert.That(this.client.Player.SceneIsReady);
        }

        [UnityTest]
        public IEnumerator ReadyExceptionTest() => UniTask.ToCoroutine(async () =>
        {
            this.sceneManager.Client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.sceneManager.Client.Active);

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.sceneManager.SetSceneIsReady();
            });
        });

        [Test]
        public void ClientChangeSceneTest()
        {
            var func1 = Substitute.For<UnityAction<string, SceneOperation>>();
            this.sceneManager.OnClientStartedSceneChange.AddListener(func1);

            this.sceneManager.OnClientStartedSceneChange.Invoke(default, SceneOperation.Normal);

            func1.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void ClientSceneChangedTest()
        {
            var func1 = Substitute.For<UnityAction<Scene, SceneOperation>>();
            this.sceneManager.OnClientFinishedSceneChange.AddListener(func1);
            this.sceneManager.OnClientFinishedSceneChange.Invoke(default, SceneOperation.Normal);
            func1.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void ClientSceneReadyAfterChangedTest()
        {
            var _readyAfterSceneChanged = false;
            this.sceneManager.OnClientFinishedSceneChange.AddListener((Scene scene, SceneOperation operation) => _readyAfterSceneChanged = this.client.Player.SceneIsReady);
            this.sceneManager.OnClientFinishedSceneChange.Invoke(default, SceneOperation.Normal);

            Assert.That(_readyAfterSceneChanged, Is.True);
        }

        [UnityTest]
        public IEnumerator ChangeSceneAdditiveLoadTest() => UniTask.ToCoroutine(async () =>
        {
            this.sceneManager.ServerLoadSceneAdditively("Assets/Mirror/Tests/Runtime/testScene.unity", new[] { this.client.Player });

            await AsyncUtil.WaitUntilWithTimeout(() => this.sceneManager.ActiveScenePath != null);

            Assert.That(this.sceneManager.ActiveScenePath, Is.Not.Null);
        });

        [Test]
        public void ClientChangeSceneNotNullTest()
        {
            Assert.That(this.sceneManager.OnClientStartedSceneChange, Is.Not.Null);
        }

        [Test]
        public void ClientSceneChangedNotNullTest()
        {
            Assert.That(this.sceneManager.OnClientFinishedSceneChange, Is.Not.Null);
        }

        [Test]
        public void ServerChangeSceneNotNullTest()
        {
            Assert.That(this.sceneManager.OnServerStartedSceneChange, Is.Not.Null);
        }

        [Test]
        public void ServerSceneChangedNotNullTest()
        {
            Assert.That(this.sceneManager.OnServerFinishedSceneChange, Is.Not.Null);
        }

        [Test]
        public void ServerCheckScenesPlayerIsInTest()
        {
            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            var scenes = this.sceneManager.ScenesPlayerIsIn(this.server.LocalPlayer);

            Assert.That(scenes, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ClientNotReadyMessageTest() => UniTask.ToCoroutine(async () =>
        {
            this.sceneManager.ClientNotReadyMessage(this.client.Player, new SceneNotReadyMessage());

            await UniTask.Delay(1);

            Assert.That(this.sceneManager.Client.Player.SceneIsReady, Is.Not.True);
        });

        [Test]
        public void ServerUnloadSceneCheckServerNotNullTest()
        {
            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            this.sceneManager.Server = null;

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                this.sceneManager.ServerUnloadSceneAdditively(SceneManager.GetActiveScene(), new[] { this.server.LocalPlayer });
            });

            var message = new InvalidOperationException("Method can only be called if server is active").Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }

        [Test]
        public void ServerUnloadSceneAdditivelySceneNotNullTest()
        {
            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                this.sceneManager.ServerUnloadSceneAdditively(default, null);
            });

            var message = new ArgumentNullException("scene", "[NetworkSceneManager] - ServerChangeScene: " + "scene" + " cannot be null").Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }


        [Test]
        public void ServerUnloadSceneAdditivelyPlayersNotNullTest()
        {
            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                this.sceneManager.ServerUnloadSceneAdditively(SceneManager.GetActiveScene(), null);
            });

            var message = new ArgumentNullException("players", "[NetworkSceneManager] - list of player's cannot be null or no players.").Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }

        [UnityTest]
        public IEnumerator ServerUnloadSceneAdditivelyListenerInvokedTest() => UniTask.ToCoroutine(async () =>
        {
            var _invokedOnServerStartedSceneChange = false;

            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

#if UNITY_EDITOR
            await EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Tests/Performance/Runtime/10K/Scenes/Scene.unity", new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive });
#else
            throw new System.NotSupportedException("Test not supported in player");
#endif

            this.sceneManager.OnServerStartedSceneChange.AddListener((arg0, operation) => _invokedOnServerStartedSceneChange = true);

            this.sceneManager.ServerUnloadSceneAdditively(SceneManager.GetActiveScene(), new[] { this.server.LocalPlayer });

            await AsyncUtil.WaitUntilWithTimeout(() => _invokedOnServerStartedSceneChange);

            Assert.That(_invokedOnServerStartedSceneChange, Is.True);
        });

        [Test]
        public void ServerSceneLoadingNullPlayersCheckTest()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                this.sceneManager.ServerLoadSceneAdditively("Assets/Mirror/Tests/Runtime/testScene.unity", null);
            });

            var message = new ArgumentNullException("players", "No player's were added to send for information").Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }

        [Test]
        public void IsPlayerInSceneThrowForInvalidScene()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                this.sceneManager.IsPlayerInScene(default, this.server.LocalPlayer);
            });

            var message = new ArgumentException("Scene is not valid", "scene").Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }
        [Test]
        public void IsPlayerInSceneThrowForNotFoundScene()
        {
            var scene = SceneManager.CreateScene("Not Found Scene");
            var exception = Assert.Throws<KeyNotFoundException>(() =>
            {
                this.sceneManager.IsPlayerInScene(scene, this.server.LocalPlayer);
            });

            var message = new KeyNotFoundException($"Could not find player list for scene:{scene}").Message;
            Assert.That(exception, Has.Message.EqualTo(message));

            // cleanup
            SceneManager.UnloadSceneAsync(scene);
        }


        [UnityTest]
        public IEnumerator OnServerDisconnectPlayerTest() => UniTask.ToCoroutine(async () =>
        {
            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            await AsyncUtil.WaitUntilWithTimeout(() => this.sceneManager.ServerSceneData.Count > 0);

            this.sceneManager.OnServerPlayerDisconnected(this.sceneManager.Server.Players.ElementAt(0));

            Assert.That(this.sceneManager.IsPlayerInScene(this.sceneManager.ServerSceneData.ElementAt(0).Key,
                this.server.Players.ElementAt(0)), Is.False);
        });

        [UnityTest]
        public IEnumerator IsPlayerInSceneTest() => UniTask.ToCoroutine(async () =>
        {
            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");

            await AsyncUtil.WaitUntilWithTimeout(() => this.sceneManager.ServerSceneData.Count > 0);

            Assert.That(this.sceneManager.IsPlayerInScene(this.sceneManager.ServerSceneData.ElementAt(0).Key, this.server.Players.ElementAt(0)),
                Is.True);
        });
    }
}

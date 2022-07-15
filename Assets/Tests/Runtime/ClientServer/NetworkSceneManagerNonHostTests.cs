using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using InvalidEnumArgumentException = System.ComponentModel.InvalidEnumArgumentException;

namespace Mirage.Tests.Runtime.ClientServer
{

    [TestFixture]
    public class NetworkSceneManagerNonHostTests : ClientServerSetup<MockComponent>
    {
        private AssetBundle bundle;

        public override void ExtraSetup()
        {
            this.bundle = AssetBundle.LoadFromFile("Assets/Tests/Runtime/TestScene/testscene");
        }

        public override void ExtraTearDown()
        {
            UnloadAdditiveScenes();

            this.bundle.Unload(true);
        }

        private static void UnloadAdditiveScenes()
        {
            var active = SceneManager.GetActiveScene();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (active == scene) { continue; }
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        [Test]
        public void ClientSceneMessageExceptionTest()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                this.clientSceneManager.ClientStartSceneMessage(null, new SceneMessage());
            });
        }

        [Test]
        public void FinishLoadSceneTest()
        {
            var func2 = Substitute.For<UnityAction<Scene, SceneOperation>>();
            this.clientSceneManager.OnClientFinishedSceneChange.AddListener(func2);
            this.clientSceneManager.CompleteLoadingScene(default, SceneOperation.Normal);

            func2.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
        }

        [UnityTest]
        public IEnumerator ClientOfflineSceneException() => UniTask.ToCoroutine(async () =>
        {
            this.client.Disconnect();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.Active);

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.clientSceneManager.ClientStartSceneMessage(null, new SceneMessage());
            });
        });

        [UnityTest]
        public IEnumerator ClientSceneMessageInvokeTest() => UniTask.ToCoroutine(async () =>
        {
            var startInvoked = 0;
            var endInvoked = 0;

            this.clientSceneManager.OnClientStartedSceneChange.AddListener((_, __) => startInvoked++);
            this.clientSceneManager.OnClientFinishedSceneChange.AddListener((_, __) => endInvoked++);
            this.clientSceneManager.ClientStartSceneMessage(null, new SceneMessage { MainActivateScene = "Assets/Mirror/Tests/Runtime/testScene.unity" });

            await AsyncUtil.WaitUntilWithTimeout(() => startInvoked == 1);

            // wait 1/2 a second to see if end invokes itself
            await UniTask.Delay(500);
            Assert.That(startInvoked == 1, "Start should only be called once");
            Assert.That(endInvoked == 0, "Should wait for ready before end is called");

            this.clientSceneManager.ClientFinishedLoadingSceneMessage(this.clientPlayer, new SceneReadyMessage());

            await AsyncUtil.WaitUntilWithTimeout(() => endInvoked == 1);

            Assert.That(this.clientSceneManager.ActiveScenePath, Is.EqualTo("Assets/Mirror/Tests/Runtime/testScene.unity"));

            Assert.That(startInvoked == 1, "Start should only be called once");
            Assert.That(endInvoked == 1, "End should only be called once");
        });

        [Test]
        public void ClientSceneMessageThrowsIfInvalidSceneOperation()
        {
            var startInvoked = 0;
            var endInvoked = 0;

            this.clientSceneManager.OnClientStartedSceneChange.AddListener((_, __) => startInvoked++);
            this.clientSceneManager.OnClientFinishedSceneChange.AddListener((_, __) => endInvoked++);

            var invalidOperation = (SceneOperation)10;
            var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
            {
                this.clientSceneManager.ClientStartSceneMessage(null, new SceneMessage
                {
                    MainActivateScene = "Assets/Mirror/Tests/Runtime/testScene.unity",
                    SceneOperation = invalidOperation
                });
            });

            var message = new InvalidEnumArgumentException("sceneOperation", 10, typeof(SceneOperation)).Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }

        [Test]
        public void NetworkSceneNameStringValueTest()
        {
            Assert.That(this.clientSceneManager.ActiveScenePath.Equals(SceneManager.GetActiveScene().path));
        }

        [Test]
        public void ServerChangeSceneTest()
        {
            var func1 = Substitute.For<UnityAction<string, SceneOperation>>();
            this.serverSceneManager.OnServerStartedSceneChange.AddListener(func1);
            this.serverSceneManager.OnServerStartedSceneChange.Invoke("test", SceneOperation.Normal);
            func1.Received(1).Invoke(Arg.Any<string>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void ServerSceneChangedTest()
        {
            var func1 = Substitute.For<UnityAction<Scene, SceneOperation>>();
            this.serverSceneManager.OnServerFinishedSceneChange.AddListener(func1);
            this.serverSceneManager.OnServerFinishedSceneChange.Invoke(default, SceneOperation.Normal);
            func1.Received(1).Invoke(Arg.Any<Scene>(), Arg.Any<SceneOperation>());
        }

        [Test]
        public void OnClientSceneLoadFinishedTest()
        {
            this.clientSceneManager.ClientPendingAdditiveSceneLoadingList.Add(null);

            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                this.clientSceneManager.OnClientSceneLoadFinished(default, SceneOperation.Normal);
            });

            var message = new ArgumentNullException("ClientPendingAdditiveSceneLoadingList[0]", "Some how a null scene path has been entered.").Message;
            Assert.That(exception, Has.Message.EqualTo(message));
        }

        [UnityTest]
        public IEnumerator OnClientSceneChangedAdditiveListTest() => UniTask.ToCoroutine(async () =>
        {
            this.clientSceneManager.OnClientFinishedSceneChange.AddListener(this.CheckForPendingAdditiveSceneList);
            this.clientSceneManager.ClientStartSceneMessage(this.client.Player, new SceneMessage { MainActivateScene = "Assets/Mirror/Tests/Runtime/testScene.unity", AdditiveScenes = new List<string> { "Assets/Mirror/Tests/Runtime/testScene.unity" } });

            await AsyncUtil.WaitUntilWithTimeout(() => this.noAdditiveScenesFound);

            Assert.That(this.noAdditiveScenesFound);
        });

        private bool noAdditiveScenesFound;

        private void CheckForPendingAdditiveSceneList(Scene scene, SceneOperation sceneOperation)
        {
            if (this.clientSceneManager.ClientPendingAdditiveSceneLoadingList.Count == 0)
            {
                this.noAdditiveScenesFound = true;
            }
        }

        [Test]
        public void ClientSceneMessagePendingAdditiveSceneListTest()
        {
            //Check for the additive scene in the pending list at the time of ClientChangeScene before its removed as part of it being loaded.
            this.clientSceneManager.OnClientStartedSceneChange.AddListener(this.CheckForAdditiveScene);
            this.clientSceneManager.ClientStartSceneMessage(this.client.Player, new SceneMessage { MainActivateScene = "Assets/Mirror/Tests/Runtime/testScene.unity", AdditiveScenes = new List<string> { "Assets/Mirror/Tests/Runtime/testScene.unity" } });

            Assert.That(this.additiveSceneWasFound);
        }

        private bool additiveSceneWasFound;

        private void CheckForAdditiveScene(string scenePath, SceneOperation sceneOperation)
        {
            if (this.clientSceneManager.ClientPendingAdditiveSceneLoadingList.Contains("Assets/Mirror/Tests/Runtime/testScene.unity"))
            {
                this.additiveSceneWasFound = true;
            }
        }
    }
}

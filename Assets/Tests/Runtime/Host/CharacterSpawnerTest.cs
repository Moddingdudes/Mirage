using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    public class CharacterSpawnerTest : HostSetup<MockComponent>
    {
        private AssetBundle bundle;
        private GameObject player;
        private CharacterSpawner spawner;

        public override void ExtraSetup()
        {
            this.bundle = AssetBundle.LoadFromFile("Assets/Tests/Runtime/TestScene/testscene");

            this.spawner = this.networkManagerGo.AddComponent<CharacterSpawner>();

            this.spawner.Client = this.client;
            this.spawner.Server = this.server;
            this.spawner.SceneManager = this.sceneManager;
            this.spawner.ClientObjectManager = this.clientObjectManager;
            this.spawner.ServerObjectManager = this.serverObjectManager;

            this.player = new GameObject();
            var identity = this.player.AddComponent<NetworkIdentity>();
            this.spawner.PlayerPrefab = identity;

            this.spawner.AutoSpawn = false;
        }

        public override void ExtraTearDown()
        {
            this.bundle.Unload(true);
            Object.Destroy(this.player);
        }

        [UnityTest]
        public IEnumerator DontAutoSpawnTest() => UniTask.ToCoroutine(async () =>
        {
            var invokeAddPlayerMessage = false;
            this.ServerMessageHandler.RegisterHandler<AddCharacterMessage>(msg => invokeAddPlayerMessage = true);

            this.sceneManager.ServerLoadSceneNormal("Assets/Mirror/Tests/Runtime/testScene.unity");
            // wait for messages to be processed
            await UniTask.Yield();

            Assert.That(invokeAddPlayerMessage, Is.False);

        });

        [UnityTest]
        public IEnumerator ManualSpawnTest() => UniTask.ToCoroutine(async () =>
        {
            var invokeAddPlayerMessage = false;
            this.ServerMessageHandler.RegisterHandler<AddCharacterMessage>(msg => invokeAddPlayerMessage = true);

            this.spawner.RequestServerSpawnPlayer();

            // wait for messages to be processed
            await UniTask.Yield();

            Assert.That(invokeAddPlayerMessage, Is.True);
        });
    }
}

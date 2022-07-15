using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mirage.Tests
{

    public class CharacterSpawnerTest
    {
        private GameObject go;
        private NetworkClient client;
        private NetworkServer server;
        private CharacterSpawner spawner;
        private NetworkSceneManager sceneManager;
        private ServerObjectManager serverObjectManager;
        private ClientObjectManager clientObjectManager;
        private GameObject playerPrefab;

        private Transform pos1;
        private Transform pos2;

        [SetUp]
        public void Setup()
        {
            this.go = new GameObject();
            this.client = this.go.AddComponent<NetworkClient>();
            this.server = this.go.AddComponent<NetworkServer>();
            this.spawner = this.go.AddComponent<CharacterSpawner>();
            this.sceneManager = this.go.AddComponent<NetworkSceneManager>();
            this.serverObjectManager = this.go.AddComponent<ServerObjectManager>();
            this.clientObjectManager = this.go.AddComponent<ClientObjectManager>();
            this.spawner.SceneManager = this.sceneManager;
            this.sceneManager.Client = this.client;
            this.sceneManager.Server = this.server;
            this.serverObjectManager.Server = this.server;
            this.clientObjectManager.Client = this.client;
            this.clientObjectManager.NetworkSceneManager = this.sceneManager;
            this.spawner.Client = this.client;
            this.spawner.Server = this.server;
            this.spawner.ServerObjectManager = this.serverObjectManager;
            this.spawner.ClientObjectManager = this.clientObjectManager;

            this.playerPrefab = new GameObject();
            var playerId = this.playerPrefab.AddComponent<NetworkIdentity>();

            this.spawner.PlayerPrefab = playerId;

            this.pos1 = new GameObject().transform;
            this.pos2 = new GameObject().transform;
            this.spawner.startPositions.Add(this.pos1);
            this.spawner.startPositions.Add(this.pos2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(this.go);
            Object.DestroyImmediate(this.playerPrefab);

            Object.DestroyImmediate(this.pos1.gameObject);
            Object.DestroyImmediate(this.pos2.gameObject);
        }

        [Test]
        public void StartExceptionTest()
        {
            this.spawner.PlayerPrefab = null;
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.spawner.Awake();
            });
        }

        [Test]
        public void StartExceptionMissingServerObjectManagerTest()
        {
            this.spawner.ServerObjectManager = null;
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.spawner.Awake();
            });
        }

        [Test]
        public void AutoConfigureClient()
        {
            this.spawner.Awake();
            Assert.That(this.spawner.Client, Is.SameAs(this.client));
        }

        [Test]
        public void AutoConfigureServer()
        {
            this.spawner.Awake();
            Assert.That(this.spawner.Server, Is.SameAs(this.server));
        }

        [Test]
        public void GetStartPositionRoundRobinTest()
        {
            this.spawner.Awake();

            this.spawner.playerSpawnMethod = CharacterSpawner.PlayerSpawnMethod.RoundRobin;
            Assert.That(this.spawner.GetStartPosition(), Is.SameAs(this.pos1.transform));
            Assert.That(this.spawner.GetStartPosition(), Is.SameAs(this.pos2.transform));
            Assert.That(this.spawner.GetStartPosition(), Is.SameAs(this.pos1.transform));
            Assert.That(this.spawner.GetStartPosition(), Is.SameAs(this.pos2.transform));
        }

        [Test]
        public void GetStartPositionRandomTest()
        {
            this.spawner.Awake();

            this.spawner.playerSpawnMethod = CharacterSpawner.PlayerSpawnMethod.Random;
            Assert.That(this.spawner.GetStartPosition(), Is.SameAs(this.pos1.transform) | Is.SameAs(this.pos2.transform));
        }

        [Test]
        public void GetStartPositionNullTest()
        {
            this.spawner.Awake();

            this.spawner.startPositions.Clear();
            Assert.That(this.spawner.GetStartPosition(), Is.SameAs(null));
        }

        [Test]
        public void MissingClientObjectSpawnerExceptionTest()
        {
            this.spawner.ClientObjectManager = null;

            Assert.Throws<InvalidOperationException>(() =>
            {
                this.spawner.OnClientConnected(null);
            });
        }
    }
}

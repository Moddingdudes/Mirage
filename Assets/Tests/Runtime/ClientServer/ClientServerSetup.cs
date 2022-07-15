using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirage.SocketLayer;
using UnityEngine;
using UnityEngine.TestTools;

using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class ClientServerSetup<T> where T : NetworkBehaviour
    {
        protected GameObject serverGo;
        protected NetworkServer server;
        protected NetworkSceneManager serverSceneManager;
        protected ServerObjectManager serverObjectManager;
        protected GameObject serverPlayerGO;
        protected NetworkIdentity serverIdentity;
        protected T serverComponent;

        protected GameObject clientGo;
        protected NetworkClient client;
        protected NetworkSceneManager clientSceneManager;
        protected ClientObjectManager clientObjectManager;
        protected GameObject clientPlayerGO;
        protected NetworkIdentity clientIdentity;
        protected T clientComponent;

        protected GameObject playerPrefab;

        protected TestSocketFactory socketFactory;
        protected INetworkPlayer clientPlayer;
        /// <summary>
        /// network player instance on server that represents the client
        /// <para>NOT the local player</para>
        /// </summary>
        protected INetworkPlayer serverPlayer;
        protected MessageHandler ClientMessageHandler => this.client.MessageHandler;
        protected MessageHandler ServerMessageHandler => this.server.MessageHandler;

        public virtual void ExtraSetup() { }
        public virtual UniTask LateSetup() => UniTask.CompletedTask;

        protected virtual bool AutoConnectClient => true;
        protected virtual Config ServerConfig => null;
        protected virtual Config ClientConfig => null;

        protected List<GameObject> toDestroy = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator Setup() => UniTask.ToCoroutine(async () =>
        {
            this.serverGo = new GameObject("server", typeof(NetworkSceneManager), typeof(ServerObjectManager), typeof(NetworkServer));
            this.clientGo = new GameObject("client", typeof(NetworkSceneManager), typeof(ClientObjectManager), typeof(NetworkClient));
            this.socketFactory = this.serverGo.AddComponent<TestSocketFactory>();

            await UniTask.Delay(1);

            this.server = this.serverGo.GetComponent<NetworkServer>();
            this.client = this.clientGo.GetComponent<NetworkClient>();

            if (this.ServerConfig != null) this.server.PeerConfig = this.ServerConfig;
            if (this.ClientConfig != null) this.client.PeerConfig = this.ClientConfig;

            this.server.SocketFactory = this.socketFactory;
            this.client.SocketFactory = this.socketFactory;

            this.serverSceneManager = this.serverGo.GetComponent<NetworkSceneManager>();
            this.clientSceneManager = this.clientGo.GetComponent<NetworkSceneManager>();
            this.serverSceneManager.Server = this.server;
            this.clientSceneManager.Client = this.client;
            this.serverSceneManager.Start();
            this.clientSceneManager.Start();

            this.serverObjectManager = this.serverGo.GetComponent<ServerObjectManager>();
            this.serverObjectManager.Server = this.server;
            this.serverObjectManager.NetworkSceneManager = this.serverSceneManager;
            this.serverObjectManager.Start();

            this.clientObjectManager = this.clientGo.GetComponent<ClientObjectManager>();
            this.clientObjectManager.Client = this.client;
            this.clientObjectManager.NetworkSceneManager = this.clientSceneManager;
            this.clientObjectManager.Start();

            this.ExtraSetup();

            // create and register a prefab
            this.playerPrefab = new GameObject("player (unspawned)", typeof(NetworkIdentity), typeof(T));
            var identity = this.playerPrefab.GetComponent<NetworkIdentity>();
            identity.PrefabHash = Guid.NewGuid().GetHashCode();
            this.clientObjectManager.RegisterPrefab(identity);

            // wait for client and server to initialize themselves
            await UniTask.Delay(1);

            // start the server
            var started = new UniTaskCompletionSource();
            this.server.Started.AddListener(() => started.TrySetResult());
            this.server.StartServer();

            await started.Task;

            if (this.AutoConnectClient)
            {
                // now start the client
                this.client.Connect("localhost");

                await AsyncUtil.WaitUntilWithTimeout(() => this.server.Players.Count > 0);

                // get the connections so that we can spawn players
                this.serverPlayer = this.server.Players.First();
                this.clientPlayer = this.client.Player;

                // create a player object in the server
                this.serverPlayerGO = Object.Instantiate(this.playerPrefab);
                this.serverPlayerGO.name = "player (server)";
                this.serverIdentity = this.serverPlayerGO.GetComponent<NetworkIdentity>();
                this.serverComponent = this.serverPlayerGO.GetComponent<T>();
                this.serverObjectManager.AddCharacter(this.serverPlayer, this.serverPlayerGO);

                // wait for client to spawn it
                await AsyncUtil.WaitUntilWithTimeout(() => this.clientPlayer.HasCharacter);

                this.clientIdentity = this.clientPlayer.Identity;
                this.clientPlayerGO = this.clientIdentity.gameObject;
                this.clientPlayerGO.name = "player (client)";
                this.clientComponent = this.clientPlayerGO.GetComponent<T>();
            }

            await this.LateSetup();
        });

        public virtual void ExtraTearDown() { }

        [UnityTearDown]
        public IEnumerator ShutdownHost() => UniTask.ToCoroutine(async () =>
        {
            // check active, it might have been stopped by tests
            if (this.client.Active) this.client.Disconnect();
            if (this.server.Active) this.server.Stop();

            await AsyncUtil.WaitUntilWithTimeout(() => !this.client.Active);
            await AsyncUtil.WaitUntilWithTimeout(() => !this.server.Active);

            Object.DestroyImmediate(this.playerPrefab);
            Object.DestroyImmediate(this.serverGo);
            Object.DestroyImmediate(this.clientGo);
            Object.DestroyImmediate(this.serverPlayerGO);
            Object.DestroyImmediate(this.clientPlayerGO);

            foreach (var obj in this.toDestroy)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            this.ExtraTearDown();
        });


        /// <summary>
        /// Instantiate object that will be destroyed in teardown
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        protected GameObject InstantiateForTest(GameObject prefab)
        {
            var obj = Object.Instantiate(prefab);
            this.toDestroy.Add(obj);
            return obj;
        }
        /// <summary>
        /// Instantiate object that will be destroyed in teardown
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        /// <param name="prefab"></param>
        /// <returns></returns>
        protected TObj InstantiateForTest<TObj>(TObj prefab) where TObj : Component
        {
            var obj = Object.Instantiate(prefab);
            this.toDestroy.Add(obj.gameObject);
            return obj;
        }

        /// <summary>
        /// Creates a new NetworkIdentity that can be used by tests, then destroyed in teardown 
        /// </summary>
        /// <returns></returns>
        protected NetworkIdentity CreateNetworkIdentity()
        {
            this.playerPrefab = new GameObject("A NetworkIdentity", typeof(NetworkIdentity));
            this.toDestroy.Add(this.playerPrefab);
            return this.playerPrefab.GetComponent<NetworkIdentity>();
        }
    }
}

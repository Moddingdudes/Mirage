using System.Collections;
using Cysharp.Threading.Tasks;
using Mirage.SocketLayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Host
{
    public class HostSetup<T> where T : NetworkBehaviour
    {
        protected GameObject networkManagerGo;
        protected NetworkManager manager;
        protected NetworkServer server;
        protected NetworkClient client;
        protected NetworkSceneManager sceneManager;
        protected ServerObjectManager serverObjectManager;
        protected ClientObjectManager clientObjectManager;

        protected GameObject playerGO;
        protected NetworkIdentity identity;
        protected T component;

        protected MessageHandler ClientMessageHandler => this.client.MessageHandler;
        protected MessageHandler ServerMessageHandler => this.server.MessageHandler;

        protected virtual bool AutoStartServer => true;
        protected virtual Config ServerConfig => null;
        protected virtual Config ClientConfig => null;

        public virtual void ExtraSetup() { }

        [UnitySetUp]
        public IEnumerator SetupHost() => UniTask.ToCoroutine(async () =>
        {
            this.networkManagerGo = new GameObject();
            // set gameobject name to test name (helps with debugging)
            this.networkManagerGo.name = TestContext.CurrentContext.Test.MethodName;

            this.networkManagerGo.AddComponent<TestSocketFactory>();
            this.sceneManager = this.networkManagerGo.AddComponent<NetworkSceneManager>();
            this.serverObjectManager = this.networkManagerGo.AddComponent<ServerObjectManager>();
            this.clientObjectManager = this.networkManagerGo.AddComponent<ClientObjectManager>();
            this.manager = this.networkManagerGo.AddComponent<NetworkManager>();
            this.server = this.networkManagerGo.AddComponent<NetworkServer>();
            this.client = this.networkManagerGo.AddComponent<NetworkClient>();
            this.manager.Client = this.networkManagerGo.GetComponent<NetworkClient>();
            this.manager.Server = this.networkManagerGo.GetComponent<NetworkServer>();

            if (this.ServerConfig != null) this.server.PeerConfig = this.ServerConfig;
            if (this.ClientConfig != null) this.client.PeerConfig = this.ClientConfig;

            this.sceneManager.Client = this.client;
            this.sceneManager.Server = this.server;
            this.serverObjectManager.Server = this.server;
            this.serverObjectManager.NetworkSceneManager = this.sceneManager;
            this.clientObjectManager.Client = this.client;
            this.clientObjectManager.NetworkSceneManager = this.sceneManager;

            this.ExtraSetup();

            // wait for all Start() methods to get invoked
            await UniTask.DelayFrame(1);

            if (this.AutoStartServer)
            {
                await this.StartHost();

                this.playerGO = new GameObject("playerGO", typeof(Rigidbody));
                this.identity = this.playerGO.AddComponent<NetworkIdentity>();
                this.component = this.playerGO.AddComponent<T>();

                this.serverObjectManager.AddCharacter(this.server.LocalPlayer, this.playerGO);

                // wait for client to spawn it
                await AsyncUtil.WaitUntilWithTimeout(() => this.client.Player.HasCharacter);
            }
        });

        protected async UniTask StartHost()
        {
            var completionSource = new UniTaskCompletionSource();

            void Started()
            {
                completionSource.TrySetResult();
            }

            this.server.Started.AddListener(Started);
            // now start the host
            this.manager.Server.StartServer(this.client);

            await completionSource.Task;
        }

        public virtual void ExtraTearDown() { }

        [UnityTearDown]
        public IEnumerator ShutdownHost() => UniTask.ToCoroutine(async () =>
        {
            Object.Destroy(this.playerGO);

            // check active, it might have been stopped by tests
            if (this.server.Active) this.server.Stop();

            await UniTask.Delay(1);
            Object.Destroy(this.networkManagerGo);

            this.ExtraTearDown();
        });

        public void DoUpdate(int updateCount = 1)
        {
            for (var i = 0; i < updateCount; i++)
            {
                this.server.Update();
                this.client.Update();
            }
        }
    }
}

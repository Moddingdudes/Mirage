using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Mirage.SocketLayer;
using Mirage.Sockets.Udp;
using UnityEngine;

namespace Mirage.HeadlessBenchmark
{
    public class HeadlessBenchmark : MonoBehaviour
    {
        public GameObject ServerPrefab;
        public GameObject ClientPrefab;
        public NetworkServer server;
        public ServerObjectManager serverObjectManager;
        public GameObject MonsterPrefab;
        public GameObject PlayerPrefab;
        public string editorArgs;
        public SocketFactory socketFactory;
        private string[] cachedArgs;
        private string port;

        private void Start()
        {
            this.cachedArgs = Application.isEditor ?
                this.cachedArgs = this.editorArgs.Split(' ') :
                Environment.GetCommandLineArgs();

            this.HeadlessStart();

        }
        private IEnumerator DisplayFramesPerSecons()
        {
            var previousFrameCount = Time.frameCount;
            long previousMessageCount = 0;

            while (true)
            {
                yield return new WaitForSeconds(1);
                var frameCount = Time.frameCount;
                var frames = frameCount - previousFrameCount;

                long messageCount = 0;
                // todo use debug metrics from peer when they are added
                //if (transport is KcpTransport kcpTransport)
                //{
                //    messageCount = kcpTransport.ReceivedMessageCount;
                //}

                var messages = messageCount - previousMessageCount;

                if (Application.isEditor)
                    Debug.LogFormat("{0} FPS {1} messages {2} clients", frames, messages, this.server.NumberOfPlayers);
                else
                    Console.WriteLine("{0} FPS {1} messages {2} clients", frames, messages, this.server.NumberOfPlayers);
                previousFrameCount = frameCount;
                previousMessageCount = messageCount;
            }
        }

        private void HeadlessStart()
        {
            //Try to find port
            this.port = this.GetArgValue("-port");

            //Try to find Socket
            this.ParseForSocket();

            //Server mode?
            this.ParseForServerMode();

            //Or client mode?
            this.StartClients().Forget();

            this.ParseForHelp();
        }

        private void OnServerStarted()
        {
            this.StartCoroutine(this.DisplayFramesPerSecons());

            var monster = this.GetArgValue("-monster");
            if (!string.IsNullOrEmpty(monster))
            {
                for (var i = 0; i < int.Parse(monster); i++)
                    this.SpawnMonsters(i);
            }
        }

        private void SpawnMonsters(int i)
        {
            var monster = Instantiate(this.MonsterPrefab);
            monster.gameObject.name = $"Monster {i}";
            this.serverObjectManager.Spawn(monster.gameObject);
        }

        private void ParseForServerMode()
        {
            if (string.IsNullOrEmpty(this.GetArg("-server"))) return;

            var serverGo = Instantiate(this.ServerPrefab);
            serverGo.name = "Server";
            this.server = serverGo.GetComponent<NetworkServer>();
            this.server.MaxConnections = 9999;
            this.server.SocketFactory = this.socketFactory;
            this.serverObjectManager = serverGo.GetComponent<ServerObjectManager>();

            var networkSceneManager = serverGo.GetComponent<NetworkSceneManager>();
            networkSceneManager.Server = this.server;

            this.serverObjectManager.Server = this.server;
            this.serverObjectManager.NetworkSceneManager = networkSceneManager;
            this.serverObjectManager.Start();

            var spawner = serverGo.GetComponent<CharacterSpawner>();
            spawner.ServerObjectManager = this.serverObjectManager;
            spawner.Server = this.server;

            this.server.Started.AddListener(this.OnServerStarted);
            this.server.Authenticated.AddListener(conn => this.serverObjectManager.SpawnVisibleObjects(conn, true));
            this.server.StartServer();
            Console.WriteLine("Starting Server Only Mode");
        }

        private async UniTaskVoid StartClients()
        {
            var clientArg = this.GetArg("-client");
            if (!string.IsNullOrEmpty(clientArg))
            {
                //network address provided?
                var address = this.GetArgValue("-address");
                if (string.IsNullOrEmpty(address))
                {
                    address = "localhost";
                }

                //nested clients
                var clonesCount = 1;
                var clonesString = this.GetArgValue("-client");
                if (!string.IsNullOrEmpty(clonesString))
                {
                    clonesCount = int.Parse(clonesString);
                }

                Console.WriteLine("Starting {0} clients", clonesCount);

                // connect from a bunch of clients
                for (var i = 0; i < clonesCount; i++)
                {
                    this.StartClient(i, address);
                    await UniTask.Delay(500);

                    Debug.LogFormat("Started {0} clients", i + 1);
                }
            }
        }

        private void StartClient(int i, string networkAddress)
        {
            var clientGo = Instantiate(this.ClientPrefab);
            clientGo.name = $"Client {i}";
            var client = clientGo.GetComponent<NetworkClient>();
            var objectManager = clientGo.GetComponent<ClientObjectManager>();
            var spawner = clientGo.GetComponent<CharacterSpawner>();
            var networkSceneManager = clientGo.GetComponent<NetworkSceneManager>();
            networkSceneManager.Client = client;

            objectManager.Client = client;
            objectManager.NetworkSceneManager = networkSceneManager;
            objectManager.Start();
            objectManager.RegisterPrefab(this.MonsterPrefab.GetComponent<NetworkIdentity>());
            objectManager.RegisterPrefab(this.PlayerPrefab.GetComponent<NetworkIdentity>());

            spawner.Client = client;
            spawner.PlayerPrefab = this.PlayerPrefab.GetComponent<NetworkIdentity>();
            spawner.ClientObjectManager = objectManager;
            spawner.SceneManager = networkSceneManager;

            client.SocketFactory = this.socketFactory;

            try
            {
                client.Connect(networkAddress);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void ParseForHelp()
        {
            if (!string.IsNullOrEmpty(this.GetArg("-help")))
            {
                Console.WriteLine("--==Mirage HeadlessClients Benchmark==--");
                Console.WriteLine("Please start your standalone application with the -nographics and -batchmode options");
                Console.WriteLine("Also provide these arguments to control the autostart process:");
                Console.WriteLine("-server (will run in server only mode)");
                Console.WriteLine("-client 1234 (will run the specified number of clients)");
                Console.WriteLine("-transport tcp (transport to be used in test. add more by editing HeadlessBenchmark.cs)");
                Console.WriteLine("-address example.com (will run the specified number of clients)");
                Console.WriteLine("-port 1234 (port used by transport)");
                Console.WriteLine("-monster 100 (number of monsters to spawn on the server)");

                Application.Quit();
            }
        }

        private void ParseForSocket()
        {
            var socket = this.GetArgValue("-socket");
            if (string.IsNullOrEmpty(socket) || socket.Equals("udp"))
            {
                var newSocket = this.gameObject.AddComponent<UdpSocketFactory>();
                this.socketFactory = newSocket;

                //Try to apply port if exists and needed by transport.

                //TODO: Uncomment this after the port is made public
                /*if (!string.IsNullOrEmpty(port))
                {
                    newSocket.port = ushort.Parse(port);
                    newSocket.
                }*/
            }
        }

        private string GetArgValue(string name)
        {
            for (var i = 0; i < this.cachedArgs.Length; i++)
            {
                if (this.cachedArgs[i] == name && this.cachedArgs.Length > i + 1)
                {
                    return this.cachedArgs[i + 1];
                }
            }
            return null;
        }

        private string GetArg(string name)
        {
            for (var i = 0; i < this.cachedArgs.Length; i++)
            {
                if (this.cachedArgs[i] == name)
                {
                    return this.cachedArgs[i];
                }
            }
            return null;
        }
    }
}

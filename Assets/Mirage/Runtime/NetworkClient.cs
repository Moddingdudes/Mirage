using System;
using Mirage.Events;
using Mirage.Logging;
using Mirage.Serialization;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage
{
    public enum ConnectState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    /// <summary>
    /// This is a network client class used by the networking system. It contains a NetworkConnection that is used to connect to a network server.
    /// <para>The <see cref="NetworkClient">NetworkClient</see> handle connection state, messages handlers, and connection configuration. There can be many <see cref="NetworkClient">NetworkClient</see> instances in a process at a time, but only one that is connected to a game server (<see cref="NetworkServer">NetworkServer</see>) that uses spawned objects.</para>
    /// <para><see cref="NetworkClient">NetworkClient</see> has an internal update function where it handles events from the transport layer. This includes asynchronous connect events, disconnect events and incoming data from a server.</para>
    /// </summary>
    [AddComponentMenu("Network/NetworkClient")]
    [DisallowMultipleComponent]
    public class NetworkClient : MonoBehaviour, INetworkClient
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkClient));

        public bool EnablePeerMetrics;
        [Tooltip("Sequence size of buffer in bits.\n10 => array size 1024 => ~17 seconds at 60hz")]
        public int MetricsSize = 10;
        public Metrics Metrics { get; private set; }

        /// <summary>
        /// Config for peer, if not set will use default settings
        /// </summary>
        public Config PeerConfig { get; set; }

        [Tooltip("Creates Socket for Peer to use")]
        public SocketFactory SocketFactory;

        public bool DisconnectOnException = true;

        [Tooltip("If true will set Application.runInBackground")]
        public bool RunInBackground = true;
        private Peer peer;

        [Tooltip("Authentication component attached to this object")]
        public NetworkAuthenticator authenticator;

        [Header("Events")]
        [SerializeField] private AddLateEvent _started = new AddLateEvent();
        [SerializeField] private NetworkPlayerAddLateEvent _connected = new NetworkPlayerAddLateEvent();
        [SerializeField] private NetworkPlayerAddLateEvent _authenticated = new NetworkPlayerAddLateEvent();
        [SerializeField] private DisconnectAddLateEvent _disconnected = new DisconnectAddLateEvent();

        /// <summary>
        /// Event fires when the client starts, before it has connected to the Server.
        /// </summary>
        public IAddLateEvent Started => this._started;

        /// <summary>
        /// Event fires once the Client has connected its Server.
        /// </summary>
        public IAddLateEvent<INetworkPlayer> Connected => this._connected;

        /// <summary>
        /// Event fires after the Client connection has successfully been authenticated with its Server.
        /// </summary>
        public IAddLateEvent<INetworkPlayer> Authenticated => this._authenticated;

        /// <summary>
        /// Event fires after the Client has disconnected from its Server and Cleanup has been called.
        /// </summary>
        public IAddLateEvent<ClientStoppedReason> Disconnected => this._disconnected;

        /// <summary>
        /// The NetworkConnection object this client is using.
        /// </summary>
        public INetworkPlayer Player { get; internal set; }

        internal ConnectState connectState = ConnectState.Disconnected;

        /// <summary>
        /// active is true while a client is connecting/connected
        /// (= while the network is active)
        /// </summary>
        public bool Active => this.connectState == ConnectState.Connecting || this.connectState == ConnectState.Connected;

        /// <summary>
        /// This gives the current connection status of the client.
        /// </summary>
        public bool IsConnected => this.connectState == ConnectState.Connected;

        public NetworkWorld World { get; private set; }
        public MessageHandler MessageHandler { get; private set; }


        /// <summary>
        /// NetworkClient can connect to local server in host mode too
        /// </summary>
        public bool IsLocalClient { get; private set; }

        /// <summary>
        /// Connect client to a NetworkServer instance.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public void Connect(string address = null, ushort? port = null)
        {
            this.ThrowIfActive();
            this.ThrowIfSocketIsMissing();

            this.connectState = ConnectState.Connecting;

            this.World = new NetworkWorld();

            var endPoint = this.SocketFactory.GetConnectEndPoint(address, port);
            if (logger.LogEnabled()) logger.Log($"Client connecting to endpoint: {endPoint}");

            var socket = this.SocketFactory.CreateClientSocket();
            var maxPacketSize = this.SocketFactory.MaxPacketSize;
            this.MessageHandler = new MessageHandler(this.World, this.DisconnectOnException);
            var dataHandler = new DataHandler(this.MessageHandler);
            this.Metrics = this.EnablePeerMetrics ? new Metrics(this.MetricsSize) : null;

            var config = this.PeerConfig ?? new Config();

            NetworkWriterPool.Configure(maxPacketSize);

            this.peer = new Peer(socket, maxPacketSize, dataHandler, config, LogFactory.GetLogger<Peer>(), this.Metrics);
            this.peer.OnConnected += this.Peer_OnConnected;
            this.peer.OnConnectionFailed += this.Peer_OnConnectionFailed;
            this.peer.OnDisconnected += this.Peer_OnDisconnected;

            var connection = this.peer.Connect(endPoint);

            if (this.RunInBackground)
                Application.runInBackground = this.RunInBackground;

            // setup all the handlers
            this.Player = new NetworkPlayer(connection);
            dataHandler.SetConnection(connection, this.Player);

            this.RegisterMessageHandlers();
            this.InitializeAuthEvents();
            // invoke started event after everything is set up, but before peer has connected
            this._started.Invoke();
        }

        private void ThrowIfActive()
        {
            if (this.Active) throw new InvalidOperationException("Client is already active");
        }

        private void ThrowIfSocketIsMissing()
        {
            if (this.SocketFactory is null)
                this.SocketFactory = this.GetComponent<SocketFactory>();
            if (this.SocketFactory == null)
                throw new InvalidOperationException($"{nameof(this.SocketFactory)} could not be found for ${nameof(NetworkServer)}");
        }

        private void Peer_OnConnected(IConnection conn)
        {
            this.World.Time.UpdateClient(this);
            this.connectState = ConnectState.Connected;
            this._connected.Invoke(this.Player);
        }

        private void Peer_OnConnectionFailed(IConnection conn, RejectReason reason)
        {
            if (logger.LogEnabled()) logger.Log($"Failed to connect to {conn.EndPoint} with reason {reason}");
            this.Player?.MarkAsDisconnected();
            this._disconnected?.Invoke(reason.ToClientStoppedReason());
            this.Cleanup();
        }

        private void Peer_OnDisconnected(IConnection conn, DisconnectReason reason)
        {
            if (logger.LogEnabled()) logger.Log($"Disconnected from {conn.EndPoint} with reason {reason}");
            this.Player?.MarkAsDisconnected();
            this._disconnected?.Invoke(reason.ToClientStoppedReason());
            this.Cleanup();
        }

        private void OnHostDisconnected()
        {
            this.Player?.MarkAsDisconnected();
            this._disconnected?.Invoke(ClientStoppedReason.HostModeStopped);
        }

        internal void ConnectHost(NetworkServer server, IDataHandler serverDataHandler)
        {
            this.ThrowIfActive();

            logger.Log("Client Connect Host to Server");
            // start connecting for setup, then "Peer_OnConnected" below will change to connected
            this.connectState = ConnectState.Connecting;

            this.World = server.World;

            // create local connection objects and connect them
            this.MessageHandler = new MessageHandler(this.World, this.DisconnectOnException);
            var dataHandler = new DataHandler(this.MessageHandler);
            (var clientConn, var serverConn) = PipePeerConnection.Create(dataHandler, serverDataHandler, this.OnHostDisconnected, null);

            // set up client before connecting to server, server could invoke handlers
            this.IsLocalClient = true;
            this.Player = new NetworkPlayer(clientConn);
            dataHandler.SetConnection(clientConn, this.Player);
            this.RegisterHostHandlers();
            this.InitializeAuthEvents();
            // invoke started event after everything is set up, but before peer has connected
            this._started.Invoke();

            // we need add server connection to server's dictionary first
            // then invoke connected event on client (client has to connect first or it will miss message in NetworkScenemanager)
            // then invoke connected event on server

            server.AddLocalConnection(this, serverConn);
            this.Peer_OnConnected(clientConn);
            server.InvokeLocalConnected();
        }

        private void InitializeAuthEvents()
        {
            if (this.authenticator != null)
            {
                this.authenticator.OnClientAuthenticated += this.OnAuthenticated;
                this.authenticator.ClientSetup(this);

                this.Connected.AddListener(this.authenticator.ClientAuthenticate);
            }
            else
            {
                // if no authenticator, consider connection as authenticated
                this.Connected.AddListener(this.OnAuthenticated);
            }
        }

        internal void OnAuthenticated(INetworkPlayer player)
        {
            this._authenticated.Invoke(player);
        }

        private void OnDestroy()
        {
            if (this.Active)
                this.Disconnect();
        }

        /// <summary>
        /// Disconnect from server.
        /// <para>The disconnect message will be invoked.</para>
        /// </summary>
        public void Disconnect()
        {
            if (!this.Active)
            {
                logger.LogWarning("Can't disconnect client because it is not active");
                return;
            }

            this.Player.Connection.Disconnect();
            this.Cleanup();
        }

        /// <summary>
        /// This sends a network message with a message Id to the server. This message is sent on channel zero, which by default is the reliable channel.
        /// <para>The message must be an instance of a class derived from MessageBase.</para>
        /// <para>The message id passed to Send() is used to identify the handler function to invoke on the server when the message is received.</para>
        /// </summary>
        /// <typeparam name="T">The message type to unregister.</typeparam>
        /// <param name="message"></param>
        /// <param name="channelId"></param>
        /// <returns>True if message was sent.</returns>
        public void Send<T>(T message, int channelId = Channel.Reliable)
        {
            this.Player.Send(message, channelId);
        }

        public void Send(ArraySegment<byte> segment, int channelId = Channel.Reliable)
        {
            this.Player.Send(segment, channelId);
        }

        public void Send<T>(T message, INotifyCallBack notifyCallBack)
        {
            this.Player.Send(message, notifyCallBack);
        }

        internal void Update()
        {
            // local connection?
            if (!this.IsLocalClient && this.Active && this.connectState == ConnectState.Connected)
            {
                // only update things while connected
                this.World.Time.UpdateClient(this);
            }
            this.peer?.UpdateReceive();
            this.peer?.UpdateSent();
        }

        internal void RegisterHostHandlers()
        {
            this.MessageHandler.RegisterHandler<NetworkPongMessage>(msg => { });
        }

        internal void RegisterMessageHandlers()
        {
            this.MessageHandler.RegisterHandler<NetworkPongMessage>(this.World.Time.OnClientPong);
        }

        /// <summary>
        /// Shut down a client.
        /// <para>This should be done when a client is no longer going to be used.</para>
        /// </summary>
        private void Cleanup()
        {
            logger.Log("Shutting down client.");

            this.IsLocalClient = false;

            this.connectState = ConnectState.Disconnected;

            if (this.authenticator != null)
            {
                this.authenticator.OnClientAuthenticated -= this.OnAuthenticated;
                this.Connected.RemoveListener(this.authenticator.ClientAuthenticate);
            }
            else
            {
                // if no authenticator, consider connection as authenticated
                this.Connected.RemoveListener(this.OnAuthenticated);
            }

            this.Player = null;
            this._connected.Reset();
            this._authenticated.Reset();
            this._disconnected.Reset();

            if (this.peer != null)
            {
                //remove handlers first to stop loop
                this.peer.OnConnected -= this.Peer_OnConnected;
                this.peer.OnConnectionFailed -= this.Peer_OnConnectionFailed;
                this.peer.OnDisconnected -= this.Peer_OnDisconnected;
                this.peer.Close();
                this.peer = null;
            }
        }

        internal class DataHandler : IDataHandler
        {
            private IConnection connection;
            private INetworkPlayer player;
            private readonly IMessageReceiver messageHandler;

            public DataHandler(IMessageReceiver messageHandler)
            {
                this.messageHandler = messageHandler;
            }

            public void SetConnection(IConnection connection, INetworkPlayer player)
            {
                this.connection = connection;
                this.player = player;
            }

            public void ReceiveMessage(IConnection connection, ArraySegment<byte> message)
            {
                logger.Assert(this.connection == connection);
                this.messageHandler.HandleMessage(this.player, message);
            }
        }
    }
}

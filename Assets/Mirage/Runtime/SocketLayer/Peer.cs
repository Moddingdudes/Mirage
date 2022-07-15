using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.SocketLayer
{
    public interface ITime
    {
        float Now { get; }
    }
    internal class Time : ITime
    {
        public float Now => UnityEngine.Time.time;
    }

    public interface IPeer
    {
        event Action<IConnection> OnConnected;
        event Action<IConnection, RejectReason> OnConnectionFailed;
        event Action<IConnection, DisconnectReason> OnDisconnected;

        void Bind(IEndPoint endPoint);
        IConnection Connect(IEndPoint endPoint);
        void Close();
        /// <summary>
        /// Call this at the start of the frame to receive new messages
        /// </summary>
        void UpdateReceive();
        /// <summary>
        /// Call this at end of frame to send new batches
        /// </summary>
        void UpdateSent();
    }

    /// <summary>
    /// Controls flow of data in/out of mirage, Uses <see cref="ISocket"/>
    /// </summary>
    public sealed class Peer : IPeer
    {
        private readonly ILogger logger;
        private readonly Metrics metrics;
        private readonly ISocket socket;
        private readonly IDataHandler dataHandler;
        private readonly Config config;
        private readonly int maxPacketSize;
        private readonly Time time;
        private readonly ConnectKeyValidator connectKeyValidator;
        private readonly Pool<ByteBuffer> bufferPool;
        private readonly Dictionary<IEndPoint, Connection> connections = new Dictionary<IEndPoint, Connection>();

        // list so that remove can take place after foreach loops
        private readonly List<Connection> connectionsToRemove = new List<Connection>();

        public event Action<IConnection> OnConnected;
        public event Action<IConnection, DisconnectReason> OnDisconnected;
        public event Action<IConnection, RejectReason> OnConnectionFailed;

        /// <summary>
        /// is server listening on or connected to endpoint
        /// </summary>
        private bool active;

        public Peer(ISocket socket, int maxPacketSize, IDataHandler dataHandler, Config config = null, ILogger logger = null, Metrics metrics = null)
        {
            this.logger = logger;
            this.metrics = metrics;
            this.config = config ?? new Config();
            this.maxPacketSize = maxPacketSize;
            if (maxPacketSize < AckSystem.MIN_RELIABLE_HEADER_SIZE + 1)
                throw new ArgumentException($"Max packet size too small for AckSystem header", nameof(maxPacketSize));

            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            this.time = new Time();

            this.connectKeyValidator = new ConnectKeyValidator(this.config.key);

            this.bufferPool = new Pool<ByteBuffer>(ByteBuffer.CreateNew, maxPacketSize, this.config.BufferPoolStartSize, this.config.BufferPoolMaxSize, this.logger);
            Application.quitting += this.Application_quitting;
        }

        private void Application_quitting()
        {
            // make sure peer closes itself when applications closes.
            // this will make sure that disconnect Command is sent before applications closes
            if (this.active)
                this.Close();
        }

        public void Bind(IEndPoint endPoint)
        {
            if (this.active) throw new InvalidOperationException("Peer is already active");
            this.active = true;
            this.socket.Bind(endPoint);
        }

        public IConnection Connect(IEndPoint endPoint)
        {
            if (this.active) throw new InvalidOperationException("Peer is already active");
            if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));

            this.active = true;
            this.socket.Connect(endPoint);

            var connection = this.CreateNewConnection(endPoint);
            connection.State = ConnectionState.Connecting;

            // update now to send connectRequest command
            connection.Update();
            return connection;
        }

        public void Close()
        {
            if (!this.active)
            {
                if (this.logger.Enabled(LogType.Warning)) this.logger.Log(LogType.Warning, "Peer is not active");
                return;
            }
            this.active = false;
            Application.quitting -= this.Application_quitting;

            // send disconnect messages
            foreach (var conn in this.connections.Values)
            {
                conn.Disconnect(DisconnectReason.RequestedByLocalPeer);
            }
            this.RemoveConnections();

            // close socket
            this.socket.Close();
        }

        internal void Send(Connection connection, byte[] data, int length)
        {
            // connecting connections can send connect messages so is allowed
            // todo check connected before message are sent from high level
            this.logger?.Assert(connection.State == ConnectionState.Connected || connection.State == ConnectionState.Connecting || connection.State == ConnectionState.Disconnected, connection.State);

            this.socket.Send(connection.EndPoint, data, length);
            this.metrics?.OnSend(length);
            connection.SetSendTime();

            if (this.logger.Enabled(LogType.Log))
            {
                if ((PacketType)data[0] == PacketType.Command)
                {
                    this.logger.Log($"Send to {connection} type: Command, {(Commands)data[1]}");
                }
                else
                {
                    this.logger.Log($"Send to {connection} type: {(PacketType)data[0]}");
                }
            }
        }

        internal void SendUnreliable(Connection connection, byte[] packet, int offset, int length)
        {
            using (var buffer = this.bufferPool.Take())
            {
                Buffer.BlockCopy(packet, offset, buffer.array, 1, length);
                // set header
                buffer.array[0] = (byte)PacketType.Unreliable;

                this.Send(connection, buffer.array, length + 1);
            }
        }

        internal void SendCommandUnconnected(IEndPoint endPoint, Commands command, byte? extra = null)
        {
            using (var buffer = this.bufferPool.Take())
            {
                var length = this.CreateCommandPacket(buffer, command, extra);

                this.socket.Send(endPoint, buffer.array, length);
                this.metrics?.OnSendUnconnected(length);
                if (this.logger.Enabled(LogType.Log))
                {
                    this.logger.Log($"Send to {endPoint} type: Command, {command}");
                }
            }
        }

        internal void SendConnectRequest(Connection connection)
        {
            using (var buffer = this.bufferPool.Take())
            {
                var length = this.CreateCommandPacket(buffer, Commands.ConnectRequest, null);
                this.connectKeyValidator.CopyTo(buffer.array);
                this.Send(connection, buffer.array, length + this.connectKeyValidator.KeyLength);
            }
        }

        internal void SendCommand(Connection connection, Commands command, byte? extra = null)
        {
            using (var buffer = this.bufferPool.Take())
            {
                var length = this.CreateCommandPacket(buffer, command, extra);
                this.Send(connection, buffer.array, length);
            }
        }

        /// <summary>
        /// Create a command packet from command and extra data
        /// </summary>
        /// <param name="buffer">buffer to write to</param>
        /// <param name="command"></param>
        /// <param name="extra">optional extra data as 3rd byte</param>
        /// <returns>length written</returns>
        private int CreateCommandPacket(ByteBuffer buffer, Commands command, byte? extra = null)
        {
            buffer.array[0] = (byte)PacketType.Command;
            buffer.array[1] = (byte)command;

            if (extra.HasValue)
            {
                buffer.array[2] = extra.Value;
                return 3;
            }
            else
            {
                return 2;
            }
        }

        internal void SendKeepAlive(Connection connection)
        {
            using (var buffer = this.bufferPool.Take())
            {
                buffer.array[0] = (byte)PacketType.KeepAlive;
                this.Send(connection, buffer.array, 1);
            }
        }

        /// <summary>
        /// Call this at the start of the frame to receive new messages
        /// </summary>
        public void UpdateReceive()
        {
            this.ReceiveLoop();
        }
        /// <summary>
        /// Call this at end of frame to send new batches
        /// </summary>
        public void UpdateSent()
        {
            this.UpdateConnections();
            this.metrics?.OnTick(this.connections.Count);
        }


        private void ReceiveLoop()
        {
            using (var buffer = this.bufferPool.Take())
            {
                while (this.socket.Poll())
                {
                    var length = this.socket.Receive(buffer.array, out var receiveEndPoint);

                    // this should never happen. buffer size is only MTU, if socket returns higher length then it has a bug.
                    if (length > this.maxPacketSize)
                        throw new IndexOutOfRangeException($"Socket returned length above MTU. MaxPacketSize:{this.maxPacketSize} length:{length}");

                    var packet = new Packet(buffer, length);

                    if (this.connections.TryGetValue(receiveEndPoint, out var connection))
                    {
                        this.metrics?.OnReceive(length);
                        this.HandleMessage(connection, packet);
                    }
                    else
                    {
                        this.metrics?.OnReceiveUnconnected(length);
                        this.HandleNewConnection(receiveEndPoint, packet);
                    }

                    // socket might have been closed by message handler
                    if (!this.active) { break; }
                }
            }
        }

        private void HandleMessage(Connection connection, Packet packet)
        {
            // ingore message of invalid size
            if (!packet.IsValidSize())
            {
                if (this.logger.Enabled(LogType.Log))
                {
                    this.logger.Log($"Receive from {connection} was too small");
                }
                return;
            }

            if (this.logger.Enabled(LogType.Log))
            {
                if (packet.type == PacketType.Command)
                {
                    this.logger.Log($"Receive from {connection} type: Command, {packet.command}");
                }
                else
                {
                    this.logger.Log($"Receive from {connection} type: {packet.type}");
                }
            }

            if (!connection.Connected)
            {
                // if not connected then we can only handle commands
                if (packet.type == PacketType.Command)
                {
                    this.HandleCommand(connection, packet);
                    connection.SetReceiveTime();

                }
                else if (this.logger.Enabled(LogType.Warning)) this.logger.Log(LogType.Warning, $"Receive from {connection} type: {packet.type} while not connected");

                // ignore other messages if not connected
                return;
            }

            // handle message when connected
            switch (packet.type)
            {
                case PacketType.Command:
                    this.HandleCommand(connection, packet);
                    break;
                case PacketType.Unreliable:
                    connection.ReceiveUnreliablePacket(packet);
                    break;
                case PacketType.Notify:
                    connection.ReceiveNotifyPacket(packet);
                    break;
                case PacketType.Reliable:
                    connection.ReceiveReliablePacket(packet);
                    break;
                case PacketType.Ack:
                    connection.ReceiveNotifyAck(packet);
                    break;
                case PacketType.ReliableFragment:
                    connection.ReceiveReliableFragment(packet);
                    break;
                case PacketType.KeepAlive:
                    // do nothing
                    break;
                default:
                    // ignore invalid PacketType
                    // return not break, so that receive time is not set for invalid packet
                    return;
            }

            connection.SetReceiveTime();
        }

        private void HandleCommand(Connection connection, Packet packet)
        {
            switch (packet.command)
            {
                case Commands.ConnectRequest:
                    this.HandleConnectionRequest(connection);
                    break;
                case Commands.ConnectionAccepted:
                    this.HandleConnectionAccepted(connection);
                    break;
                case Commands.ConnectionRejected:
                    this.HandleConnectionRejected(connection, packet);
                    break;
                case Commands.Disconnect:
                    this.HandleConnectionDisconnect(connection, packet);
                    break;
                default:
                    // ignore invalid command
                    break;
            }
        }

        private void HandleNewConnection(IEndPoint endPoint, Packet packet)
        {
            // if invalid, then reject without reason
            if (!this.Validate(packet)) { return; }


            if (!this.connectKeyValidator.Validate(packet.buffer.array))
            {
                this.RejectConnectionWithReason(endPoint, RejectReason.KeyInvalid);
            }
            else if (this.AtMaxConnections())
            {
                this.RejectConnectionWithReason(endPoint, RejectReason.ServerFull);
            }
            // todo do other security stuff here:
            // - white/black list for endpoint?
            // (maybe a callback for developers to use?)
            else
            {
                this.AcceptNewConnection(endPoint);
            }
        }

        private bool Validate(Packet packet)
        {
            // key could be anything, so any message over 2 could be key.
            var minLength = 2;
            if (packet.length < minLength)
                return false;

            if (packet.type != PacketType.Command)
                return false;

            if (packet.command != Commands.ConnectRequest)
                return false;

            return true;
        }

        private bool AtMaxConnections()
        {
            return this.connections.Count >= this.config.MaxConnections;
        }
        private void AcceptNewConnection(IEndPoint endPoint)
        {
            if (this.logger.Enabled(LogType.Log)) this.logger.Log($"Accepting new connection from:{endPoint}");

            var connection = this.CreateNewConnection(endPoint);

            this.HandleConnectionRequest(connection);
        }

        private Connection CreateNewConnection(IEndPoint _newEndPoint)
        {
            // create copy of endpoint for this connection
            // this is so that we can re-use the endpoint (reduces alloc) for receive and not worry about changing internal data needed for each connection
            var endPoint = _newEndPoint?.CreateCopy();

            var connection = new Connection(this, endPoint, this.dataHandler, this.config, this.maxPacketSize, this.time, this.bufferPool, this.logger, this.metrics);
            connection.SetReceiveTime();
            this.connections.Add(endPoint, connection);
            return connection;
        }

        private void HandleConnectionRequest(Connection connection)
        {
            switch (connection.State)
            {
                case ConnectionState.Created:
                    // mark as connected, send message, then invoke event
                    connection.State = ConnectionState.Connected;
                    this.SendCommand(connection, Commands.ConnectionAccepted);
                    OnConnected?.Invoke(connection);
                    break;

                case ConnectionState.Connected:
                    // send command again, unreliable so first message could have been missed
                    this.SendCommand(connection, Commands.ConnectionAccepted);
                    break;

                case ConnectionState.Connecting:
                    this.logger?.Error($"Server connections should not be in {nameof(ConnectionState.Connecting)} state");
                    break;
            }
        }


        private void RejectConnectionWithReason(IEndPoint endPoint, RejectReason reason)
        {
            this.SendCommandUnconnected(endPoint, Commands.ConnectionRejected, (byte)reason);
        }

        private void HandleConnectionAccepted(Connection connection)
        {
            switch (connection.State)
            {
                case ConnectionState.Created:
                    this.logger?.Error($"Accepted Connections should not be in {nameof(ConnectionState.Created)} state");
                    break;

                case ConnectionState.Connected:
                    // ignore this, command may have been re-sent or Received twice
                    break;

                case ConnectionState.Connecting:
                    connection.State = ConnectionState.Connected;
                    OnConnected?.Invoke(connection);
                    break;
            }
        }

        private void HandleConnectionRejected(Connection connection, Packet packet)
        {
            switch (connection.State)
            {
                case ConnectionState.Connecting:
                    var reason = (RejectReason)packet.buffer.array[2];
                    this.FailedToConnect(connection, reason);
                    break;

                default:
                    this.logger?.Error($"Rejected Connections should not be in {nameof(ConnectionState.Created)} state");
                    break;
            }
        }

        /// <summary>
        /// Called by connection when it is disconnected
        /// </summary>
        internal void OnConnectionDisconnected(Connection connection, DisconnectReason reason, bool sendToOther)
        {
            if (sendToOther)
            {
                // if reason is ByLocal, then change it to ByRemote for sending
                var byteReason = (byte)(reason == DisconnectReason.RequestedByLocalPeer
                    ? DisconnectReason.RequestedByRemotePeer
                    : reason);
                this.SendCommand(connection, Commands.Disconnect, byteReason);
            }

            // tell high level
            OnDisconnected?.Invoke(connection, reason);
        }

        internal void FailedToConnect(Connection connection, RejectReason reason)
        {
            if (this.logger.Enabled(LogType.Warning)) this.logger.Log(LogType.Warning, $"Connection Failed to connect: {reason}");

            this.RemoveConnection(connection);

            // tell high level
            OnConnectionFailed?.Invoke(connection, reason);
        }

        internal void RemoveConnection(Connection connection)
        {
            // shouldn't be trying to removed a destroyed connected
            this.logger?.Assert(connection.State != ConnectionState.Destroyed && connection.State != ConnectionState.Removing);

            connection.State = ConnectionState.Removing;
            this.connectionsToRemove.Add(connection);
        }

        private void HandleConnectionDisconnect(Connection connection, Packet packet)
        {
            var reason = (DisconnectReason)packet.buffer.array[2];
            connection.Disconnect(reason, false);
        }

        private void UpdateConnections()
        {
            foreach (var connection in this.connections.Values)
            {
                connection.Update();

                // was closed while in conn.Update
                // dont continue loop,
                if (!this.active) { return; }
            }

            this.RemoveConnections();
        }

        private void RemoveConnections()
        {
            if (this.connectionsToRemove.Count == 0)
                return;

            foreach (var connection in this.connectionsToRemove)
            {
                var removed = this.connections.Remove(connection.EndPoint);
                connection.State = ConnectionState.Destroyed;

                // value should be removed from dictionary
                if (!removed)
                {
                    this.logger?.Error($"Failed to remove {connection} from connection set");
                }
            }
            this.connectionsToRemove.Clear();
        }
    }
}

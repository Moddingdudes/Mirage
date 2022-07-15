using System;
using UnityEngine;

namespace Mirage.SocketLayer
{
    /// <summary>
    /// Connection for <see cref="Peer"/>
    /// </summary>
    public interface IConnection
    {
        IEndPoint EndPoint { get; }
        ConnectionState State { get; }

        void Disconnect();

        INotifyToken SendNotify(byte[] packet);
        INotifyToken SendNotify(byte[] packet, int offset, int length);
        INotifyToken SendNotify(ArraySegment<byte> packet);

        void SendNotify(byte[] packet, INotifyCallBack callBacks);
        void SendNotify(byte[] packet, int offset, int length, INotifyCallBack callBacks);
        void SendNotify(ArraySegment<byte> packet, INotifyCallBack callBacks);

        /// <summary>
        /// single message, batched by AckSystem
        /// </summary>
        /// <param name="message"></param>
        void SendReliable(byte[] message);
        /// <summary>
        /// single message, batched by AckSystem
        /// </summary>
        /// <param name="message"></param>
        void SendReliable(byte[] message, int offset, int length);
        /// <summary>
        /// single message, batched by AckSystem
        /// </summary>
        /// <param name="message"></param>
        void SendReliable(ArraySegment<byte> message);

        void SendUnreliable(byte[] packet);
        void SendUnreliable(byte[] packet, int offset, int length);
        void SendUnreliable(ArraySegment<byte> packet);

        /// <summary>
        /// Forces the connection to send any batched message immediately to the socket
        /// <para>
        /// Note: this will only send the packet to the socket. Some sockets may not send on main thread so might not send immediately
        /// </para>
        /// </summary>
        void FlushBatch();
    }

    /// <summary>
    /// A connection that can send data directly to sockets
    /// <para>Only things inside socket layer should be sending raw packets. Others should use the methods inside <see cref="Connection"/></para>
    /// </summary>
    internal interface IRawConnection
    {
        /// <summary>
        /// Sends directly to socket without adding header
        /// <para>packet given to this function as assumed to already have a header</para>
        /// </summary>
        /// <param name="packet">header and messages</param>
        void SendRaw(byte[] packet, int length);
    }

    /// <summary>
    /// Objects that represends a connection to/from a server/client. Holds state that is needed to update, send, and receive data
    /// </summary>
    internal sealed class Connection : IConnection, IRawConnection
    {
        private readonly ILogger logger;
        private ConnectionState _state;
        public ConnectionState State
        {
            get => this._state;
            set
            {
                // check new state is allowed for current state
                switch (value)
                {
                    case ConnectionState.Connected:
                        this.logger?.Assert(this._state == ConnectionState.Created || this._state == ConnectionState.Connecting);
                        break;

                    case ConnectionState.Connecting:
                        this.logger?.Assert(this._state == ConnectionState.Created);
                        break;

                    case ConnectionState.Disconnected:
                        this.logger?.Assert(this._state == ConnectionState.Connected);
                        break;

                    case ConnectionState.Destroyed:
                        this.logger?.Assert(this._state == ConnectionState.Removing);
                        break;
                }

                if (this.logger.Enabled(LogType.Log)) this.logger.Log($"{this.EndPoint} changed state from {this._state} to {value}");
                this._state = value;
            }
        }
        public bool Connected => this.State == ConnectionState.Connected;

        private readonly Peer peer;
        public readonly IEndPoint EndPoint;
        private readonly IDataHandler dataHandler;

        private readonly ConnectingTracker connectingTracker;
        private readonly TimeoutTracker timeoutTracker;
        private readonly KeepAliveTracker keepAliveTracker;
        private readonly DisconnectedTracker disconnectedTracker;

        private readonly Metrics metrics;
        private readonly AckSystem ackSystem;

        IEndPoint IConnection.EndPoint => this.EndPoint;

        internal Connection(Peer peer, IEndPoint endPoint, IDataHandler dataHandler, Config config, int maxPacketSize, Time time, Pool<ByteBuffer> bufferPool, ILogger logger, Metrics metrics)
        {
            this.peer = peer;
            this.logger = logger;

            this.EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            this.State = ConnectionState.Created;

            this.connectingTracker = new ConnectingTracker(config, time);
            this.timeoutTracker = new TimeoutTracker(config, time);
            this.keepAliveTracker = new KeepAliveTracker(config, time);
            this.disconnectedTracker = new DisconnectedTracker(config, time);

            this.metrics = metrics;
            this.ackSystem = new AckSystem(this, config, maxPacketSize, time, bufferPool, metrics);
        }

        public override string ToString()
        {
            return $"[{this.EndPoint}]";
        }

        /// <summary>
        /// Checks if new message need to be sent using its <see cref="State"/>
        /// <para>Call this at end of frame to send new batches</para>
        /// </summary>
        public void Update()
        {
            switch (this.State)
            {
                case ConnectionState.Connecting:
                    this.UpdateConnecting();
                    break;

                case ConnectionState.Connected:
                    this.UpdateConnected();
                    break;

                case ConnectionState.Disconnected:
                    this.UpdateDisconnected();
                    break;
            }
        }

        public void SetReceiveTime()
        {
            this.timeoutTracker.SetReceiveTime();
        }
        public void SetSendTime()
        {
            this.keepAliveTracker.SetSendTime();
        }

        private void ThrowIfNotConnected()
        {
            if (this._state != ConnectionState.Connected)
                throw new InvalidOperationException("Connection is not connected");
        }


        public void SendUnreliable(byte[] packet, int offset, int length)
        {
            this.ThrowIfNotConnected();
            this.metrics?.OnSendMessageUnreliable(length);
            this.peer.SendUnreliable(this, packet, offset, length);
        }
        public void SendUnreliable(byte[] packet)
        {
            this.SendUnreliable(packet, 0, packet.Length);
        }
        public void SendUnreliable(ArraySegment<byte> packet)
        {
            this.SendUnreliable(packet.Array, packet.Offset, packet.Count);
        }

        /// <summary>
        /// Use <see cref="INotifyCallBack"/> version for non-alloc
        /// </summary>
        public INotifyToken SendNotify(byte[] packet, int offset, int length)
        {
            this.ThrowIfNotConnected();
            this.metrics?.OnSendMessageNotify(length);
            return this.ackSystem.SendNotify(packet, offset, length);
        }
        /// <summary>
        /// Use <see cref="INotifyCallBack"/> version for non-alloc
        /// </summary>
        public INotifyToken SendNotify(byte[] packet)
        {
            return this.SendNotify(packet, 0, packet.Length);
        }
        /// <summary>
        /// Use <see cref="INotifyCallBack"/> version for non-alloc
        /// </summary>
        public INotifyToken SendNotify(ArraySegment<byte> packet)
        {
            return this.SendNotify(packet.Array, packet.Offset, packet.Count);
        }

        /// <summary>
        /// Use <see cref="INotifyCallBack"/> version for non-alloc
        /// </summary>
        public void SendNotify(byte[] packet, int offset, int length, INotifyCallBack callBacks)
        {
            this.ThrowIfNotConnected();
            this.metrics?.OnSendMessageNotify(length);
            this.ackSystem.SendNotify(packet, offset, length, callBacks);
        }
        /// <summary>
        /// Use <see cref="INotifyCallBack"/> version for non-alloc
        /// </summary>
        public void SendNotify(byte[] packet, INotifyCallBack callBacks)
        {
            this.SendNotify(packet, 0, packet.Length, callBacks);
        }
        /// <summary>
        /// Use <see cref="INotifyCallBack"/> version for non-alloc
        /// </summary>
        public void SendNotify(ArraySegment<byte> packet, INotifyCallBack callBacks)
        {
            this.SendNotify(packet.Array, packet.Offset, packet.Count, callBacks);
        }


        /// <summary>
        /// single message, batched by AckSystem
        /// </summary>
        /// <param name="message"></param>
        public void SendReliable(byte[] message, int offset, int length)
        {
            this.ThrowIfNotConnected();
            this.metrics?.OnSendMessageReliable(length);
            this.ackSystem.SendReliable(message, offset, length);
        }
        public void SendReliable(byte[] packet)
        {
            this.SendReliable(packet, 0, packet.Length);
        }
        public void SendReliable(ArraySegment<byte> packet)
        {
            this.SendReliable(packet.Array, packet.Offset, packet.Count);
        }


        void IRawConnection.SendRaw(byte[] packet, int length)
        {
            this.peer.Send(this, packet, length);
        }

        /// <summary>
        /// starts disconnecting this connection
        /// </summary>
        public void Disconnect()
        {
            this.Disconnect(DisconnectReason.RequestedByLocalPeer);
        }
        internal void Disconnect(DisconnectReason reason, bool sendToOther = true)
        {
            if (this.logger.Enabled(LogType.Log)) this.logger.Log($"Disconnect with reason: {reason}");
            switch (this.State)
            {
                case ConnectionState.Connecting:
                    this.peer.FailedToConnect(this, RejectReason.ClosedByPeer);
                    break;

                case ConnectionState.Connected:
                    this.State = ConnectionState.Disconnected;
                    this.disconnectedTracker.OnDisconnect();
                    this.peer.OnConnectionDisconnected(this, reason, sendToOther);
                    break;

                default:
                    break;
            }
        }

        internal void ReceiveUnreliablePacket(Packet packet)
        {
            var offset = 1;
            var count = packet.length - offset;
            var segment = new ArraySegment<byte>(packet.buffer.array, offset, count);
            this.metrics?.OnReceiveMessageUnreliable(count);
            this.dataHandler.ReceiveMessage(this, segment);
        }

        internal void ReceiveReliablePacket(Packet packet)
        {
            this.ackSystem.ReceiveReliable(packet.buffer.array, packet.length, false);

            this.HandleQueuedMessages();
        }

        internal void ReceiveReliableFragment(Packet packet)
        {
            if (this.ackSystem.InvalidFragment(packet.buffer.array))
            {
                this.Disconnect(DisconnectReason.InvalidPacket);
                return;
            }

            this.ackSystem.ReceiveReliable(packet.buffer.array, packet.length, true);

            this.HandleQueuedMessages();
        }

        private void HandleQueuedMessages()
        {
            // gets messages in order
            while (this.ackSystem.NextReliablePacket(out var received))
            {
                if (received.isFragment)
                {
                    this.HandleFragmentedMessage(received);
                }
                else
                {
                    this.HandleBatchedMessageInPacket(received);
                }
            }
        }

        private void HandleFragmentedMessage(AckSystem.ReliableReceived received)
        {
            // get index from first
            var firstArray = received.buffer.array;
            // length +1 because zero indexed 
            var fragmentLength = firstArray[0] + 1;

            // todo find way to remove allocation? (can't use buffers because they will be too small for this bigger message)
            var message = new byte[fragmentLength * this.ackSystem.SizePerFragment];

            // copy first
            var copyLength = received.length - 1;
            this.logger?.Assert(copyLength == this.ackSystem.SizePerFragment, "First should be max size");
            Buffer.BlockCopy(firstArray, 1, message, 0, copyLength);
            received.buffer.Release();

            var messageLength = copyLength;
            // start at 1 because first copied above
            for (var i = 1; i < fragmentLength; i++)
            {
                var next = this.ackSystem.GetNextFragment();
                var nextArray = next.buffer.array;

                this.logger?.Assert(i == (fragmentLength - 1 - nextArray[0]), "fragment index should decrement each time");

                // +1 because first is copied above
                copyLength = next.length - 1;
                Buffer.BlockCopy(nextArray, 1, message, this.ackSystem.SizePerFragment * i, copyLength);
                messageLength += copyLength;
                next.buffer.Release();
            }

            this.metrics?.OnReceiveMessageReliable(messageLength);
            this.dataHandler.ReceiveMessage(this, new ArraySegment<byte>(message, 0, messageLength));
        }

        private void HandleBatchedMessageInPacket(AckSystem.ReliableReceived received)
        {
            var array = received.buffer.array;
            var packetLength = received.length;
            var offset = 0;
            while (offset < packetLength)
            {
                var length = ByteUtils.ReadUShort(array, ref offset);
                var message = new ArraySegment<byte>(array, offset, length);
                offset += length;

                this.metrics?.OnReceiveMessageReliable(length);
                this.dataHandler.ReceiveMessage(this, message);
            }

            // release buffer after all its message have been handled
            received.buffer.Release();
        }

        internal void ReceiveNotifyPacket(Packet packet)
        {
            var segment = this.ackSystem.ReceiveNotify(packet.buffer.array, packet.length);
            if (segment != default)
            {
                this.metrics?.OnReceiveMessageNotify(packet.length);
                this.dataHandler.ReceiveMessage(this, segment);
            }
        }

        internal void ReceiveNotifyAck(Packet packet)
        {
            this.ackSystem.ReceiveAck(packet.buffer.array);
        }


        /// <summary>
        /// client connecting attempts
        /// </summary>
        private void UpdateConnecting()
        {
            if (this.connectingTracker.TimeAttempt())
            {
                if (this.connectingTracker.MaxAttempts())
                {
                    this.peer.FailedToConnect(this, RejectReason.Timeout);
                }
                else
                {
                    this.connectingTracker.OnAttempt();
                    this.peer.SendConnectRequest(this);
                }
            }
        }

        /// <summary>
        /// Used to remove connection after it has been disconnected
        /// </summary>
        private void UpdateDisconnected()
        {
            if (this.disconnectedTracker.TimeToRemove())
            {
                this.peer.RemoveConnection(this);
            }
        }

        void IConnection.FlushBatch()
        {
            this.ackSystem.Update();
        }

        /// <summary>
        /// Used to keep connection alive
        /// </summary>
        private void UpdateConnected()
        {
            if (this.timeoutTracker.TimeToDisconnect())
            {
                this.Disconnect(DisconnectReason.Timeout);
            }

            this.ackSystem.Update();

            if (this.keepAliveTracker.TimeToSend())
            {
                this.peer.SendKeepAlive(this);
            }
        }

        private class ConnectingTracker
        {
            private readonly Config config;
            private readonly Time time;
            private float lastAttempt = float.MinValue;
            private int AttemptCount = 0;

            public ConnectingTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time;
            }

            public bool TimeAttempt()
            {
                return this.lastAttempt + this.config.ConnectAttemptInterval < this.time.Now;
            }

            public bool MaxAttempts()
            {
                return this.AttemptCount >= this.config.MaxConnectAttempts;
            }

            public void OnAttempt()
            {
                this.AttemptCount++;
                this.lastAttempt = this.time.Now;
            }
        }

        private class TimeoutTracker
        {
            private float lastRecvTime = float.MinValue;
            private readonly Config config;
            private readonly Time time;

            public TimeoutTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time ?? throw new ArgumentNullException(nameof(time));
            }

            public bool TimeToDisconnect()
            {
                return this.lastRecvTime + this.config.TimeoutDuration < this.time.Now;
            }

            public void SetReceiveTime()
            {
                this.lastRecvTime = this.time.Now;
            }
        }

        private class KeepAliveTracker
        {
            private float lastSendTime = float.MinValue;
            private readonly Config config;
            private readonly Time time;

            public KeepAliveTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time ?? throw new ArgumentNullException(nameof(time));
            }


            public bool TimeToSend()
            {
                return this.lastSendTime + this.config.KeepAliveInterval < this.time.Now;
            }

            public void SetSendTime()
            {
                this.lastSendTime = this.time.Now;
            }
        }

        private class DisconnectedTracker
        {
            private bool isDisonnected;
            private float disconnectTime;
            private readonly Config config;
            private readonly Time time;

            public DisconnectedTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time ?? throw new ArgumentNullException(nameof(time));
            }

            public void OnDisconnect()
            {
                this.disconnectTime = this.time.Now + this.config.DisconnectDuration;
                this.isDisonnected = true;
            }

            public bool TimeToRemove()
            {
                return this.isDisonnected && this.disconnectTime < this.time.Now;
            }
        }
    }
}

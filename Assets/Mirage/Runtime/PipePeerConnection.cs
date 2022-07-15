using System;
using Mirage.Logging;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage
{
    /// <summary>
    /// A <see cref="IConnection"/> that is directly sends data to a <see cref="IDataHandler"/>
    /// </summary>
    public class PipePeerConnection : IConnection
    {
        private static readonly ILogger logger = LogFactory.GetLogger<PipePeerConnection>();

        /// <summary>
        /// handler of other connection
        /// </summary>
        private IDataHandler otherHandler;

        /// <summary>
        /// other connection that is passed to handler
        /// </summary>
        private IConnection otherConnection;

        /// <summary>
        /// Name used for debugging
        /// </summary>
        private string name;
        private Action OnDisconnect;

        private PipePeerConnection() { }

        public static (IConnection clientConn, IConnection serverConn) Create(IDataHandler clientHandler, IDataHandler serverHandler, Action ClientOnDisconnect, Action ServerOnDisconnect)
        {
            var client = new PipePeerConnection();
            client.OnDisconnect = ClientOnDisconnect;
            var server = new PipePeerConnection();
            server.OnDisconnect = ServerOnDisconnect;

            client.otherHandler = serverHandler ?? throw new ArgumentNullException(nameof(serverHandler));
            server.otherHandler = clientHandler ?? throw new ArgumentNullException(nameof(clientHandler));

            client.otherConnection = server;
            server.otherConnection = client;

            client.State = ConnectionState.Connected;
            server.State = ConnectionState.Connected;

            client.name = "[Client Pipe Connection]";
            server.name = "[Server Pipe Connection]";

            return (client, server);
        }

        public override string ToString()
        {
            return this.name;
        }

        IEndPoint IConnection.EndPoint => new PipeEndPoint();
        void IConnection.FlushBatch() { }

        public ConnectionState State { get; private set; } = ConnectionState.Connected;

        void IConnection.Disconnect()
        {
            if (this.State == ConnectionState.Disconnected)
                return;

            this.State = ConnectionState.Disconnected;
            this.OnDisconnect?.Invoke();

            // tell other connection to also disconnect
            this.otherConnection.Disconnect();
        }

        public INotifyToken SendNotify(byte[] packet, int offset, int length)
        {
            if (this.State == ConnectionState.Disconnected)
                return default;

            this.receive(packet, offset, length);

            return new PipeNotifyToken();
        }
        public INotifyToken SendNotify(ArraySegment<byte> packet) => this.SendNotify(packet.Array, packet.Offset, packet.Count);
        public INotifyToken SendNotify(byte[] packet) => this.SendNotify(packet, 0, packet.Length);

        public void SendNotify(byte[] packet, int offset, int length, INotifyCallBack callBacks)
        {
            if (this.State == ConnectionState.Disconnected)
                return;

            this.receive(packet, offset, length);

            callBacks.OnDelivered();
        }
        public void SendNotify(ArraySegment<byte> packet, INotifyCallBack callBacks) => this.SendNotify(packet.Array, packet.Offset, packet.Count, callBacks);
        public void SendNotify(byte[] packet, INotifyCallBack callBacks) => this.SendNotify(packet, 0, packet.Length, callBacks);


        public void SendReliable(byte[] message, int offset, int length)
        {
            if (this.State == ConnectionState.Disconnected)
                return;

            this.receive(message, offset, length);
        }
        public void SendReliable(ArraySegment<byte> packet) => this.SendReliable(packet.Array, packet.Offset, packet.Count);
        public void SendReliable(byte[] packet) => this.SendReliable(packet, 0, packet.Length);


        public void SendUnreliable(byte[] packet, int offset, int length)
        {
            if (this.State == ConnectionState.Disconnected)
                return;

            this.receive(packet, offset, length);
        }
        public void SendUnreliable(ArraySegment<byte> packet) => this.SendUnreliable(packet.Array, packet.Offset, packet.Count);
        public void SendUnreliable(byte[] packet) => this.SendUnreliable(packet, 0, packet.Length);

        private void receive(byte[] packet, int offset, int length)
        {
            logger.Assert(this.State == ConnectionState.Connected);
            this.otherHandler.ReceiveMessage(this.otherConnection, new ArraySegment<byte>(packet, offset, length));
        }

        public class PipeEndPoint : IEndPoint
        {
            IEndPoint IEndPoint.CreateCopy()
            {
                // never need copy of pipeendpoint
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Token that invokes <see cref="INotifyToken.Delivered"/> immediately
        /// </summary>
        public struct PipeNotifyToken : INotifyToken
        {
            public event Action Delivered
            {
                add
                {
                    value.Invoke();
                }
                remove
                {
                    // nothing
                }
            }
            public event Action Lost
            {
                add
                {
                    // nothing
                }
                remove
                {
                    // nothing
                }
            }
        }

    }
}

#if UNITY_STANDALONE || UNITY_EDITOR
using System;
using Mirage.SocketLayer;
using NanoSockets;

namespace Mirage.Sockets.Udp
{
    // todo Create an Exception in mirage that can be re-used by multiple sockets (makes it easier for user to catch)
    public class NanoSocketException : Exception
    {
        public NanoSocketException(string message) : base(message) { }
    }
    public sealed class NanoSocket : ISocket, IDisposable
    {
        private Socket socket;
        private NanoEndPoint receiveEndPoint;
        private readonly int bufferSize;
        private bool needsDisposing;

        public NanoSocket(UdpSocketFactory factory)
        {
            this.bufferSize = factory.BufferSize;
        }
        ~NanoSocket()
        {
            this.Dispose();
        }

        private void InitSocket()
        {
            this.socket = UDP.Create(this.bufferSize, this.bufferSize);
            UDP.SetDontFragment(this.socket);
            UDP.SetNonBlocking(this.socket);
            this.needsDisposing = true;
        }

        public void Bind(IEndPoint endPoint)
        {
            this.receiveEndPoint = (NanoEndPoint)endPoint;

            this.InitSocket();
            var result = UDP.Bind(this.socket, ref this.receiveEndPoint.address);
            if (result != 0)
            {
                throw new NanoSocketException("Socket Bind failed: address or port might already be in use");
            }
        }

        public void Dispose()
        {
            if (!this.needsDisposing) return;
            UDP.Destroy(ref this.socket);
            this.needsDisposing = false;
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Connect(IEndPoint endPoint)
        {
            this.receiveEndPoint = (NanoEndPoint)endPoint;

            this.InitSocket();
            var result = UDP.Connect(this.socket, ref this.receiveEndPoint.address);
            if (result != 0)
            {
                throw new NanoSocketException("Socket Connect failed");
            }
        }

        public bool Poll()
        {
            return UDP.Poll(this.socket, 0) > 0;
        }

        public int Receive(byte[] buffer, out IEndPoint endPoint)
        {
            var count = UDP.Receive(this.socket, ref this.receiveEndPoint.address, buffer, buffer.Length);
            endPoint = this.receiveEndPoint;

            return count;
        }

        public void Send(IEndPoint endPoint, byte[] packet, int length)
        {
            var nanoEndPoint = (NanoEndPoint)endPoint;
            UDP.Send(this.socket, ref nanoEndPoint.address, packet, length);
        }
    }
}
#endif

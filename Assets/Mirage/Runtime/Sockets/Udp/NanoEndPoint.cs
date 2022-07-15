#if UNITY_STANDALONE || UNITY_EDITOR
using System;
using Mirage.SocketLayer;
using NanoSockets;

namespace Mirage.Sockets.Udp
{
    public sealed class NanoEndPoint : IEndPoint, IEquatable<NanoEndPoint>
    {
        public Address address;

        public NanoEndPoint(string host, ushort port)
        {
            this.address = new Address();
            this.address.port = port;
            UDP.SetHostName(ref this.address, host);
        }

        public NanoEndPoint(Address address)
        {
            this.address = address;
        }

        public IEndPoint CreateCopy()
        {
            return new NanoEndPoint(this.address);
        }

        public bool Equals(NanoEndPoint other)
        {
            return this.address.Equals(other.address);
        }

        public override bool Equals(object obj)
        {
            if (obj is NanoEndPoint endPoint)
            {
                return this.address.Equals(endPoint.address);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.address.GetHashCode();
        }

        public override string ToString()
        {
            return this.address.ToString();
        }
    }
}
#endif

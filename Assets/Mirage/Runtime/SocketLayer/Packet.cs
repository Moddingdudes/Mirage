using System;

namespace Mirage.SocketLayer
{
    /// <summary>
    /// Received packet
    /// <para>contains raw data and helper methods to help process that data</para>
    /// </summary>
    internal struct Packet
    {
        public readonly ByteBuffer buffer;
        public readonly int length;

        public Packet(ByteBuffer data, int length)
        {
            this.buffer = data ?? throw new ArgumentNullException(nameof(data));
            this.length = length;
        }

        public bool IsValidSize()
        {
            const int MinPacketSize = 1;

            if (this.length < MinPacketSize)
                return false;

            // Min size of message given to Mirage
            const int MIN_MESSAGE_SIZE = 2;


            const int MIN_COMMAND_SIZE = 2;
            const int MIN_UNRELIABLE_SIZE = 1 + MIN_MESSAGE_SIZE;

            switch (this.type)
            {
                case PacketType.Command:
                    return this.length >= MIN_COMMAND_SIZE;

                case PacketType.Unreliable:
                    return this.length >= MIN_UNRELIABLE_SIZE;

                case PacketType.Notify:
                    return this.length >= AckSystem.NOTIFY_HEADER_SIZE + MIN_MESSAGE_SIZE;
                case PacketType.Reliable:
                    return this.length >= AckSystem.MIN_RELIABLE_HEADER_SIZE + MIN_MESSAGE_SIZE;
                case PacketType.ReliableFragment:
                    return this.length >= AckSystem.MIN_RELIABLE_FRAGMENT_HEADER_SIZE + 1;
                case PacketType.Ack:
                    return this.length >= AckSystem.ACK_HEADER_SIZE;

                default:
                case PacketType.KeepAlive:
                    return true;
            }
        }

        public PacketType type => (PacketType)this.buffer.array[0];
        public Commands command => (Commands)this.buffer.array[1];
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Mirage.SocketLayer
{
    internal class AckSystem
    {
        private const int MASK_SIZE = sizeof(ulong) * 8;

        public const int SEQUENCE_HEADER = sizeof(byte) + sizeof(ushort) + sizeof(ushort) + sizeof(ulong);

        /// <summary>PacketType, sequence, ack sequence, mask</summary>
        public const int NOTIFY_HEADER_SIZE = SEQUENCE_HEADER;
        /// <summary>PacketType, sequence, ack sequence, mask, order</summary>
        public const int RELIABLE_HEADER_SIZE = SEQUENCE_HEADER + sizeof(ushort);


        /// <summary>PacketType, ack sequence, mask</summary>
        public const int ACK_HEADER_SIZE = sizeof(byte) + sizeof(ushort) + sizeof(ulong);

        public const int RELIABLE_MESSAGE_LENGTH_SIZE = sizeof(ushort);
        public const int FRAGMENT_INDEX_SIZE = sizeof(byte);

        /// <summary>Smallest size a header for reliable packet, <see cref="RELIABLE_HEADER_SIZE"/> + 2 bytes per message</summary>
        public const int MIN_RELIABLE_HEADER_SIZE = RELIABLE_HEADER_SIZE + RELIABLE_MESSAGE_LENGTH_SIZE;

        /// <summary>Smallest size a header for reliable packet, <see cref="RELIABLE_HEADER_SIZE"/> + 1 byte for fragment index</summary>
        public const int MIN_RELIABLE_FRAGMENT_HEADER_SIZE = RELIABLE_HEADER_SIZE + FRAGMENT_INDEX_SIZE;
        private readonly RingBuffer<AckablePacket> sentAckablePackets;
        private readonly Sequencer reliableOrder;
        private readonly RingBuffer<ReliableReceived> reliableReceive;

        // temp list for resending when processing sentqueue
        private readonly HashSet<ReliablePacket> toResend = new HashSet<ReliablePacket>();
        private readonly IRawConnection connection;
        private readonly ITime time;
        private readonly Pool<ByteBuffer> bufferPool;
        private readonly Pool<ReliablePacket> reliablePool;
        private readonly Metrics metrics;
        private readonly int maxPacketsInSendBufferPerConnection;
        private readonly int maxPacketSize;
        private readonly float ackTimeout;

        /// <summary>how many empty acks to send</summary>
        private readonly int emptyAckLimit;
        private readonly int receivesBeforeEmpty;
        private readonly bool allowFragmented;
        private readonly int maxFragments;
        private readonly int maxFragmentsMessageSize;

        public readonly int SizePerFragment;

        /// <summary>
        /// most recent sequence received
        /// <para>will be sent with next message</para>
        /// </summary>
        private ushort LatestAckSequence;

        /// <summary>
        /// mask of recent sequences received
        /// <para>will be sent with next message</para>
        /// </summary>
        private ulong AckMask;
        private float lastSentTime;
        private ushort lastSentAck;
        private int emptyAckCount = 0;
        private ReliablePacket nextBatch;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ackTimeout">how long after last send before sending empty ack</param>
        /// <param name="time"></param>
        public AckSystem(IRawConnection connection, Config config, int maxPacketSize, ITime time, Pool<ByteBuffer> bufferPool, Metrics metrics = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            this.connection = connection;
            this.time = time;
            this.bufferPool = bufferPool;
            this.reliablePool = new Pool<ReliablePacket>(ReliablePacket.CreateNew, default, 0, config.MaxReliablePacketsInSendBufferPerConnection);
            this.metrics = metrics;

            this.ackTimeout = config.TimeBeforeEmptyAck;
            this.emptyAckLimit = config.EmptyAckLimit;
            this.receivesBeforeEmpty = config.ReceivesBeforeEmptyAck;
            this.maxPacketSize = maxPacketSize;
            this.maxPacketsInSendBufferPerConnection = config.MaxReliablePacketsInSendBufferPerConnection;

            this.maxFragments = config.MaxReliableFragments;
            this.allowFragmented = this.maxFragments >= 0;
            this.SizePerFragment = maxPacketSize - MIN_RELIABLE_FRAGMENT_HEADER_SIZE;
            this.maxFragmentsMessageSize = this.maxFragments * this.SizePerFragment;

            var size = config.SequenceSize;
            if (size > 16) throw new ArgumentOutOfRangeException("SequenceSize", size, "SequenceSize has a max value of 16");
            this.sentAckablePackets = new RingBuffer<AckablePacket>(size);
            this.reliableOrder = new Sequencer(size);
            this.reliableReceive = new RingBuffer<ReliableReceived>(size);

            // set lastest to value before 0 so that first packet will be received
            // max will be 1 less than 0
            this.LatestAckSequence = (ushort)this.sentAckablePackets.Sequencer.MoveInBounds(ulong.MaxValue);

            this.OnSend();
        }

        /// <summary>
        /// Gets next Reliable packet in order, packet consists for multiple messsages
        /// <para>[length, message, length, message, ...]</para>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>true if next packet is available</returns>
        public bool NextReliablePacket(out ReliableReceived packet)
        {
            if (!this.reliableReceive.TryPeak(out packet))
                return false;


            // normal packet (with batched messages) OR full fragmented message
            if (!packet.isFragment || this.CheckFullFragmentedMessage(packet, this.reliableReceive.Read))
            {
                this.reliableReceive.RemoveNext();
                return true;
            }

            return false;
        }

        private bool CheckFullFragmentedMessage(ReliableReceived packet, uint readIndex)
        {
            // fragment will always be first byte of message
            uint fragmentIndex = packet.buffer.array[0];

            // if fragment Index is 3 we expect 4 packets total (3 more)
            // so we check 0,1,2 packets in
            var fullMessage = true;
            for (uint i = 0; i < fragmentIndex; i++)
            {
                // check if other packets after current exist
                if (!this.reliableReceive.Exists(readIndex + i + 1))
                {
                    fullMessage = false;
                    break;
                }
            }

            return fullMessage;
        }

        public ReliableReceived GetNextFragment()
        {
            return this.reliableReceive.Dequeue();
        }

        public void Update()
        {
            if (this.nextBatch != null)
            {
                this.SendReliablePacket(this.nextBatch);
                this.nextBatch = null;
            }


            // todo send ack if not recently been sent
            // ack only packet sent if no other sent within last frame
            if (this.ShouldSendEmptyAck() && this.TimeToSendAck())
            {
                // send ack
                this.SendAck();
            }
        }

        /// <summary>
        /// resets empty ack count, this should be called after LatestAckSequence increases
        /// </summary>
        private void ResetEmptyAckCount()
        {
            this.emptyAckCount = 0;
        }

        private void CheckSendEmptyAck()
        {
            var distance = this.sentAckablePackets.Sequencer.Distance(this.LatestAckSequence, this.lastSentAck);
            if (distance > this.receivesBeforeEmpty)
            {
                this.SendAck();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TimeToSendAck()
        {
            var shouldSend = this.lastSentTime + this.ackTimeout < this.time.Now;
            return shouldSend;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldSendEmptyAck()
        {
            return this.emptyAckCount < this.emptyAckLimit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Send(byte[] final, int length)
        {
            this.connection.SendRaw(final, length);
            this.OnSend();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnSend()
        {
            this.emptyAckCount++;
            this.lastSentAck = this.LatestAckSequence;
            this.lastSentTime = this.time.Now;
        }

        private void SendAck()
        {
            using (var final = this.bufferPool.Take())
            {
                var offset = 0;

                ByteUtils.WriteByte(final.array, ref offset, (byte)PacketType.Ack);

                ByteUtils.WriteUShort(final.array, ref offset, this.LatestAckSequence);
                ByteUtils.WriteULong(final.array, ref offset, this.AckMask);

                this.connection.SendRaw(final.array, offset);
                this.Send(final.array, offset);
            }
        }

        /// <summary>
        /// Use <see cref="SendNotify(byte[], int, int, INotifyCallBack)"/> for non-alloc version
        /// </summary>
        public INotifyToken SendNotify(byte[] inPacket, int inOffset, int inLength)
        {
            var token = new NotifyToken();
            this.SendNotify(inPacket, inOffset, inLength, token);
            return token;
        }

        public void SendNotify(byte[] inPacket, int inOffset, int inLength, INotifyCallBack callBacks)
        {
            if (inLength + NOTIFY_HEADER_SIZE > this.maxPacketSize)
            {
                throw new ArgumentException($"Message is bigger than MTU, size:{inLength} but max Notify message size is {this.maxPacketSize - NOTIFY_HEADER_SIZE}");
            }
            if (this.sentAckablePackets.IsFull)
            {
                throw new InvalidOperationException("Sent queue is full");
            }

            var sequence = (ushort)this.sentAckablePackets.Enqueue(new AckablePacket(callBacks));

            using (var buffer = this.bufferPool.Take())
            {
                var outPacket = buffer.array;
                Buffer.BlockCopy(inPacket, inOffset, outPacket, NOTIFY_HEADER_SIZE, inLength);

                var outOffset = 0;

                ByteUtils.WriteByte(outPacket, ref outOffset, (byte)PacketType.Notify);

                ByteUtils.WriteUShort(outPacket, ref outOffset, sequence);
                ByteUtils.WriteUShort(outPacket, ref outOffset, this.LatestAckSequence);
                ByteUtils.WriteULong(outPacket, ref outOffset, this.AckMask);

                this.Send(outPacket, outOffset + inLength);
            }
        }



        public void SendReliable(byte[] message, int offset, int length)
        {
            if (this.sentAckablePackets.IsFull)
            {
                throw new InvalidOperationException($"Sent queue is full for {this.connection}");
            }

            if (length + MIN_RELIABLE_HEADER_SIZE > this.maxPacketSize)
            {
                if (this.allowFragmented)
                {
                    this.SendFragmented(message, offset, length);
                    return;
                }
                else
                {
                    throw new ArgumentException($"Message is bigger than MTU and fragmentation is disabled, max Reliable message size is {this.maxPacketSize - MIN_RELIABLE_HEADER_SIZE}", nameof(length));
                }
            }


            if (this.nextBatch == null)
            {
                this.nextBatch = this.CreateReliableBuffer(PacketType.Reliable);
            }

            var msgLength = length + RELIABLE_MESSAGE_LENGTH_SIZE;
            var batchLength = this.nextBatch.length;
            if (batchLength + msgLength > this.maxPacketSize)
            {
                // if full, send and create new
                this.SendReliablePacket(this.nextBatch);

                this.nextBatch = this.CreateReliableBuffer(PacketType.Reliable);
            }

            AddToBatch(this.nextBatch, message, offset, length);
        }

        /// <summary>
        /// Splits messsage into multiple packets
        /// <para>Note: this might just send 1 packet if length is equal to size.
        /// This might happen because fragmented header is 1 less that batched header</para>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        private void SendFragmented(byte[] message, int offset, int length)
        {
            if (length > this.maxFragmentsMessageSize)
            {
                throw new ArgumentException($"Message is bigger than MTU for fragmentation, max Reliable fragmented size is {this.maxFragmentsMessageSize}", nameof(length));
            }

            var fragments = Mathf.CeilToInt(length / (float)this.SizePerFragment);

            var remaining = length;
            for (var i = 0; i < fragments; i++)
            {
                var fragmentIndex = fragments - i - 1;

                var packet = this.CreateReliableBuffer(PacketType.ReliableFragment);
                var array = packet.buffer.array;
                var packetOffset = packet.length;

                ByteUtils.WriteByte(array, ref packetOffset, (byte)fragmentIndex);
                var nextLength = Math.Min(remaining, this.SizePerFragment);
                Buffer.BlockCopy(message, offset + this.SizePerFragment * i, array, packetOffset, nextLength);
                packetOffset += nextLength;
                remaining -= nextLength;

                packet.length = packetOffset;
                this.SendReliablePacket(packet);
            }
        }

        private ReliablePacket CreateReliableBuffer(PacketType packetType)
        {
            var order = (ushort)this.reliableOrder.Next();

            var packet = this.reliablePool.Take();
            var buffer = this.bufferPool.Take();

            var offset = 0;
            ByteUtils.WriteByte(buffer.array, ref offset, (byte)packetType);

            offset = SEQUENCE_HEADER;
            ByteUtils.WriteUShort(buffer.array, ref offset, order);

            packet.Setup(order, buffer, RELIABLE_HEADER_SIZE);
            return packet;
        }

        private static void AddToBatch(ReliablePacket packet, byte[] message, int offset, int length)
        {
            var array = packet.buffer.array;
            var packetOffset = packet.length;

            ByteUtils.WriteUShort(array, ref packetOffset, (ushort)length);
            Buffer.BlockCopy(message, offset, array, packetOffset, length);
            packetOffset += length;

            packet.length = packetOffset;
        }

        private void SendReliablePacket(ReliablePacket reliable)
        {
            this.ThrowIfBufferLimitReached();

            var sequence = (ushort)this.sentAckablePackets.Enqueue(new AckablePacket(reliable));

            var final = reliable.buffer.array;

            var offset = 1;

            reliable.OnSend(sequence);
            ByteUtils.WriteUShort(final, ref offset, sequence);
            ByteUtils.WriteUShort(final, ref offset, this.LatestAckSequence);
            ByteUtils.WriteULong(final, ref offset, this.AckMask);

            this.Send(final, reliable.length);
        }

        private void ThrowIfBufferLimitReached()
        {
            // greater or equal, because we are adding 1 adder this check
            if (this.sentAckablePackets.Count >= this.maxPacketsInSendBufferPerConnection)
            {
                throw new InvalidOperationException($"Max packets in send buffer reached for {this.connection}");
            }
        }


        /// <summary>
        /// Receives incoming Notify packet
        /// <para>Ignores duplicate or late packets</para>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>default or new packet to handle</returns>
        public ArraySegment<byte> ReceiveNotify(byte[] packet, int length)
        {
            // start at 1 to skip packet type
            var offset = 1;

            var sequence = ByteUtils.ReadUShort(packet, ref offset);
            var ackSequence = ByteUtils.ReadUShort(packet, ref offset);
            var ackMask = ByteUtils.ReadULong(packet, ref offset);

            var distance = this.ProcessIncomingHeader(sequence, ackSequence, ackMask);

            // duplicate or arrived late
            if (distance <= 0) { return default; }

            var segment = new ArraySegment<byte>(packet, NOTIFY_HEADER_SIZE, length - NOTIFY_HEADER_SIZE);
            return segment;
        }


        /// <summary>
        /// Checks if fragment index is less than max fragment size
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public bool InvalidFragment(byte[] array)
        {
            var offset = RELIABLE_HEADER_SIZE;
            var fragmentIndex = ByteUtils.ReadByte(array, ref offset);

            // invalid if equal to (because it should be 0 indexed)
            return fragmentIndex >= this.maxFragments;
        }

        /// <summary>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>true if there are ordered message to read</returns>
        public void ReceiveReliable(byte[] packet, int length, bool isFragment)
        {
            // start at 1 to skip packet type
            var offset = 1;

            var sequence = ByteUtils.ReadUShort(packet, ref offset);
            var ackSequence = ByteUtils.ReadUShort(packet, ref offset);
            var ackMask = ByteUtils.ReadULong(packet, ref offset);
            var reliableSequence = ByteUtils.ReadUShort(packet, ref offset);

            _ = this.ProcessIncomingHeader(sequence, ackSequence, ackMask);

            // checks acks, late message are allowed for reliable
            // but only insert lastest if later than read Index

            var reliableDistance = this.reliableReceive.DistanceToRead(reliableSequence);

            if (reliableDistance < 0)
            {
                // old packet
                return;
            }


            if (this.PacketExists(reliableSequence))
            {
                // packet already received 
                return;
            }

            // new packet
            var savedPacket = this.bufferPool.Take();
            var bufferLength = length - RELIABLE_HEADER_SIZE;
            Buffer.BlockCopy(packet, RELIABLE_HEADER_SIZE, savedPacket.array, 0, bufferLength);
            this.reliableReceive.InsertAt(reliableSequence, new ReliableReceived(savedPacket, bufferLength, isFragment));
        }

        private bool PacketExists(ushort reliableSequence)
        {
            var existing = this.reliableReceive[reliableSequence];
            return existing.buffer != null;
        }

        public void ReceiveAck(byte[] packet)
        {
            // start at 1 to skip packet type
            var offset = 1;

            var ackSequence = ByteUtils.ReadUShort(packet, ref offset);
            var ackMask = ByteUtils.ReadULong(packet, ref offset);

            this.CheckSentQueue(ackSequence, ackMask);
        }

        /// <returns>distance</returns>
        private int ProcessIncomingHeader(ushort sequence, ushort ackSequence, ulong ackMask)
        {
            var distance = (int)this.sentAckablePackets.Sequencer.Distance(sequence, this.LatestAckSequence);
            this.SetAckValues(sequence, distance);
            this.CheckSentQueue(ackSequence, ackMask);
            return distance;
        }

        private void SetAckValues(ushort sequence, long distance)
        {
            if (distance > 0)
            {
                // distance is too large to be shifted
                if (distance >= MASK_SIZE)
                {
                    // this means 63 packets have gone missingg
                    // this should never happen, but if it does then just set mask to 1
                    this.AckMask = 1;
                }
                else
                {
                    // shift mask by distance, then add 1
                    // eg distance = 2
                    // this will mean mask will be ..01
                    // which means that 1 packet was missed
                    this.AckMask = (this.AckMask << (int)distance) | 1;
                }
                this.LatestAckSequence = sequence;
            }
            else
            {
                var negativeDistance = -(int)distance;

                // distance is too large to be shifted
                if (negativeDistance >= MASK_SIZE)
                    return;

                var newAck = 1ul << negativeDistance;
                this.AckMask |= newAck;
            }

            // after receiving reset empty count and check if we should send ack right away
            this.ResetEmptyAckCount();
            this.CheckSendEmptyAck();
        }

        private void CheckSentQueue(ushort sequence, ulong mask)
        {
            // old sequence, nothing in buffer to ack/lost
            if (this.sentAckablePackets.DistanceToRead(sequence) < 0) { return; }

            this.ackMessagesInSentQueue(sequence, mask);
            this.sentAckablePackets.MoveReadToNextNonEmpty();
            this.resendMessages();
        }
        private void ackMessagesInSentQueue(ushort sequence, ulong mask)
        {
            var start = this.sentAckablePackets.Read;
            var end = this.sentAckablePackets.Write;
            var sequencer = this.sentAckablePackets.Sequencer;

            var count = sequencer.Distance(end, start);

            for (uint i = 0; i < count; i++)
            {
                var ackableSequence = (uint)sequencer.MoveInBounds(start + i);
                var ackable = this.sentAckablePackets[ackableSequence];

                if (ackable.IsNotValid())
                    continue;

                this.CheckAckablePacket(sequence, mask, ackable, ackableSequence);
            }
        }

        private void CheckAckablePacket(ushort sequence, ulong mask, AckablePacket ackable, uint ackableSequence)
        {
            var distance = (int)this.sentAckablePackets.Sequencer.Distance(sequence, ackableSequence);

            // negative distance means next is sent after last ack, so nothing to ack yet
            // no chance for it to be acked yet, so do nothing
            if (distance < 0)
                return;


            var lost = OutsideOfMask(distance) || NotInMask(distance, mask);

            if (ackable.IsNotify)
            {
                ackable.token.Notify(!lost);
                this.sentAckablePackets.RemoveAt(ackableSequence);
            }
            else
            {
                var reliablePacket = ackable.reliablePacket;
                if (lost)
                {
                    this.reliableLost(sequence, reliablePacket);
                }
                else
                {
                    this.reliableAcked(reliablePacket);
                }
            }
        }

        private void reliableAcked(ReliablePacket reliablePacket)
        {
            foreach (var seq in reliablePacket.sequences)
            {
                this.sentAckablePackets.RemoveAt(seq);
            }

            // remove from toResend incase it was added in previous loop
            this.toResend.Remove(reliablePacket);

            reliablePacket.OnAck();
        }

        private void reliableLost(ushort sequence, ReliablePacket reliablePacket)
        {
            // we dont need to resend if it has not been possible to have been acked yet

            // eg seq=99, last = 101 => dist = 99-101=-2 => sent after seq => dont resend
            // => if positive, then resend
            if (this.sentAckablePackets.Sequencer.Distance(sequence, reliablePacket.lastSequence) > 0)
            {
                this.toResend.Add(reliablePacket);
            }
        }

        private void resendMessages()
        {
            foreach (var reliable in this.toResend)
            {
                this.metrics?.OnResend(reliable.length);
                this.SendReliablePacket(reliable);
            }
            this.toResend.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool OutsideOfMask(int distance)
        {
            // if distance is 64 or greater
            // important: this check is to stop the bitshift from breaking!!
            // bit shifting only uses first 6 bits of RHS (64->0 65->1) so higher number wont shift correct and ack wrong packet
            return distance >= MASK_SIZE;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NotInMask(int distance, ulong receivedMask)
        {
            var ackBit = 1ul << distance;
            return (receivedMask & ackBit) == 0u;
        }

        private struct AckablePacket : IEquatable<AckablePacket>
        {
            public INotifyCallBack token;
            public ReliablePacket reliablePacket;

            public bool IsNotify => this.token != null;
            public bool IsReliable => this.reliablePacket != null;

            public AckablePacket(INotifyCallBack token)
            {
                this.token = token;
                this.reliablePacket = null;
            }

            public AckablePacket(ReliablePacket reliablePacket)
            {
                this.reliablePacket = reliablePacket;
                this.token = null;
            }

            public bool Equals(AckablePacket other)
            {
                return this.token == other.token &&
                    this.reliablePacket == other.reliablePacket;
            }

            /// <summary>
            /// returns true if this is default value of struct
            /// </summary>
            /// <returns></returns>
            public bool IsNotValid()
            {
                return this.token == null && this.reliablePacket == null;
            }
        }

        private class ReliablePacket
        {
            public ushort lastSequence;
            public int length;

            public ByteBuffer buffer;
            public ushort order;

            public readonly List<ushort> sequences = new List<ushort>(4);
            private readonly Pool<ReliablePacket> pool;

            public void OnSend(ushort sequence)
            {
                this.sequences.Add(sequence);
                this.lastSequence = sequence;
            }

            public void OnAck()
            {
                this.buffer.Release();
                this.pool.Put(this);
            }

            public void Setup(ushort order, ByteBuffer buffer, int length)
            {
                // reset old data
                this.lastSequence = 0;
                this.sequences.Clear();

                this.order = order;
                this.buffer = buffer;
                this.length = length;
            }

            private ReliablePacket(Pool<ReliablePacket> pool)
            {
                this.pool = pool;
            }

            public override int GetHashCode()
            {
                return this.order;
            }

            public override bool Equals(object obj)
            {
                if (obj is ReliablePacket other)
                {
                    // use order as quick check, but use list to check if they are actually equal
                    return this.order == other.order && this.sequences == other.sequences;
                }
                return false;
            }

            public static ReliablePacket CreateNew(int _size, Pool<ReliablePacket> pool)
            {
                return new ReliablePacket(pool);
            }
        }
        public struct ReliableReceived : IEquatable<ReliableReceived>
        {
            public readonly ByteBuffer buffer;
            public readonly int length;
            public readonly bool isFragment;
            public int FragmentIndex
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.buffer.array[0];
            }

            public ReliableReceived(ByteBuffer buffer, int length, bool isFragment)
            {
                this.buffer = buffer;
                this.length = length;
                this.isFragment = isFragment;
            }

            public bool Equals(ReliableReceived other)
            {
                return this.buffer == other.buffer;
            }
        }
    }
}

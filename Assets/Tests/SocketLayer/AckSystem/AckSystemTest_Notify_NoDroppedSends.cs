using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.AckSystemTests
{
    /// <summary>
    /// Send is done in setup, and then tests just valid that the sent data is correct
    /// </summary>
    [Category("SocketLayer")]
    public class AckSystemTest_Notify_NoDroppedSends : AckSystemTestBase
    {
        private const int messageCount = 5;
        private ushort maxSequence;
        private AckTestInstance instance1;
        private AckTestInstance instance2;
        private List<ArraySegment<byte>> received1;
        private List<ArraySegment<byte>> received2;

        [SetUp]
        public void SetUp()
        {
            var config = new Config();
            this.maxSequence = (ushort)((1 << config.SequenceSize) - 1);

            this.instance1 = new AckTestInstance();
            this.instance1.connection = new SubIRawConnection();
            this.instance1.ackSystem = new AckSystem(this.instance1.connection, config, MAX_PACKET_SIZE, new Time(), this.bufferPool);
            this.received1 = new List<ArraySegment<byte>>();


            this.instance2 = new AckTestInstance();
            this.instance2.connection = new SubIRawConnection();
            this.instance2.ackSystem = new AckSystem(this.instance2.connection, config, MAX_PACKET_SIZE, new Time(), this.bufferPool);
            this.received2 = new List<ArraySegment<byte>>();

            // create and send n messages
            this.instance1.messages = new List<byte[]>();
            this.instance2.messages = new List<byte[]>();
            for (var i = 0; i < messageCount; i++)
            {
                this.instance1.messages.Add(this.createRandomData(i + 1));
                this.instance2.messages.Add(this.createRandomData(i + 1));

                // send inside loop so message sending alternates between 1 and 2

                // send to conn1
                this.instance1.ackSystem.SendNotify(this.instance1.messages[i]);
                // give to instance2 from conn1
                var segment2 = this.instance2.ackSystem.ReceiveNotify(this.instance1.connection.packets[i], this.instance1.connection.packets[i].Length);
                this.received2.Add(segment2);

                // send to conn2
                this.instance2.ackSystem.SendNotify(this.instance2.messages[i]);
                // give to instance1 from conn2
                var segment1 = this.instance1.ackSystem.ReceiveNotify(this.instance2.connection.packets[i], this.instance2.connection.packets[i].Length);
                this.received1.Add(segment1);
            }

            // should have got 1 packet
            Assert.That(this.instance1.connection.packets.Count, Is.EqualTo(messageCount));
            Assert.That(this.instance2.connection.packets.Count, Is.EqualTo(messageCount));

            // should not have null data
            Assert.That(this.instance1.connection.packets, Does.Not.Contain(null));
            Assert.That(this.instance2.connection.packets, Does.Not.Contain(null));
        }


        [Test]
        public void AllPacketsShouldBeNotify()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 0;
                var packetType = ByteUtils.ReadByte(this.instance1.packet(i), ref offset);
                Assert.That((PacketType)packetType, Is.EqualTo(PacketType.Notify));
            }

            for (var i = 0; i < messageCount; i++)
            {
                var offset = 0;
                var packetType = ByteUtils.ReadByte(this.instance2.packet(i), ref offset);
                Assert.That((PacketType)packetType, Is.EqualTo(PacketType.Notify));
            }
        }

        [Test]
        public void SequenceShouldIncrementPerSystem()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 1;
                var sequance = ByteUtils.ReadUShort(this.instance1.packet(i), ref offset);
                Assert.That(sequance, Is.EqualTo(i), "sequnce should start at 1 and increment for each message");
            }

            for (var i = 0; i < messageCount; i++)
            {
                var offset = 1;
                var sequance = ByteUtils.ReadUShort(this.instance2.packet(i), ref offset);
                Assert.That(sequance, Is.EqualTo(i), "sequnce should start at 1 and increment for each message");
            }
        }

        [Test]
        public void ReceivedShouldBeEqualToLatest()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 3;
                var received = ByteUtils.ReadUShort(this.instance1.packet(i), ref offset);
                var expected = i - 1;
                if (expected == -1) expected = this.maxSequence;
                Assert.That(received, Is.EqualTo(expected), "Received should start at 0 and increment each time");
            }

            for (var i = 0; i < messageCount; i++)
            {
                var offset = 3;
                var received = ByteUtils.ReadUShort(this.instance2.packet(i), ref offset);
                Assert.That(received, Is.EqualTo(i), "Received should start at 1 (received first message before sending) and increment each time");
            }
        }
        [Test]
        public void MaskShouldBePreviousSequences()
        {
            var expectedMask = new uint[6] {
                0b0,
                0b1,
                0b11,
                0b111,
                0b1111,
                0b1_1111,
            };
            var mask = new uint[5];

            for (var i = 0; i < messageCount; i++)
            {
                var offset = 5;
                mask[i] = ByteUtils.ReadUInt(this.instance1.packet(i), ref offset);
            }
            // do 2nd loop so we can log all values to debug
            for (var i = 0; i < messageCount; i++)
            {
                Assert.That(mask[i], Is.EqualTo(expectedMask[i]), $"Received should contain previous receives\n  instance 1, index{i}\n{string.Join(",", mask.Select(x => x.ToString()))}");
            }


            // start at 1
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 5;
                mask[i] = ByteUtils.ReadUInt(this.instance2.packet(i), ref offset);
            }
            // do 2nd loop so we can log all values to debug
            for (var i = 0; i < messageCount; i++)
            {
                Assert.That(mask[i], Is.EqualTo(expectedMask[i + 1]), $"Received should contain previous receives\n  instance 2, index{i}\n{string.Join(",", mask.Select(x => x.ToString()))}");
            }
        }

        [Test]
        public void PacketShouldContainMessage()
        {
            for (var i = 0; i < messageCount; i++)
            {
                AssertAreSameFromOffsets(this.instance1.message(i), 0, this.instance1.packet(i), AckSystem.NOTIFY_HEADER_SIZE, this.instance1.message(i).Length);
            }

            for (var i = 0; i < messageCount; i++)
            {
                AssertAreSameFromOffsets(this.instance2.message(i), 0, this.instance2.packet(i), AckSystem.NOTIFY_HEADER_SIZE, this.instance2.message(i).Length);
            }
        }

        [Test]
        public void AllSegmentsShouldHaveBeenReturned()
        {
            for (var i = 0; i < messageCount; i++)
            {
                AssertAreSameFromOffsets(this.instance1.message(i), 0, this.instance1.message(i).Length, this.received2[i]);
            }

            for (var i = 0; i < messageCount; i++)
            {
                AssertAreSameFromOffsets(this.instance2.message(i), 0, this.instance2.message(i).Length, this.received1[i]);
            }
        }
    }
}

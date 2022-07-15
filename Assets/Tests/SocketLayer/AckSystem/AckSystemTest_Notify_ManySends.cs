using System.Collections.Generic;
using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.AckSystemTests
{
    /// <summary>
    /// Send is done in setup, and then tests just valid that the sent data is correct
    /// </summary>
    [Category("SocketLayer")]
    public class AckSystemTest_Notify_ManySends : AckSystemTestBase
    {
        private const int messageCount = 5;
        private AckTestInstance instance;
        private ushort maxSequence;

        [SetUp]
        public void SetUp()
        {
            var config = new Config();
            this.maxSequence = (ushort)((1 << config.SequenceSize) - 1);

            this.instance = new AckTestInstance();
            this.instance.connection = new SubIRawConnection();
            this.instance.ackSystem = new AckSystem(this.instance.connection, new Config(), MAX_PACKET_SIZE, new Time(), this.bufferPool);

            // create and send n messages
            this.instance.messages = new List<byte[]>();
            for (var i = 0; i < messageCount; i++)
            {
                this.instance.messages.Add(this.createRandomData(i + 1));
                this.instance.ackSystem.SendNotify(this.instance.messages[i]);
            }


            // should have got 1 packet
            Assert.That(this.instance.connection.packets.Count, Is.EqualTo(messageCount));

            // should not have null data
            Assert.That(this.instance.connection.packets, Does.Not.Contain(null));
        }

        [Test]
        public void PacketsShouldBeNotify()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 0;
                var packetType = ByteUtils.ReadByte(this.instance.packet(i), ref offset);
                Assert.That((PacketType)packetType, Is.EqualTo(PacketType.Notify));
            }
        }

        [Test]
        public void PacketSequenceShouldIncrement()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 1;
                var sequance = ByteUtils.ReadUShort(this.instance.packet(i), ref offset);
                Assert.That(sequance, Is.EqualTo(i), "sequnce should start at 1 and increment for each message");
            }
        }

        [Test]
        public void PacketReceivedShouldBeMax()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 3;
                var received = ByteUtils.ReadUShort(this.instance.packet(i), ref offset);
                Assert.That(received, Is.EqualTo(this.maxSequence), $"Received should stay max, index:{i}");
            }
        }
        [Test]
        public void PacketMaskShouldBe0()
        {
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 5;
                var mask = ByteUtils.ReadUShort(this.instance.packet(i), ref offset);
                Assert.That(mask, Is.EqualTo(0), "Received should stay 0");
            }
        }

        [Test]
        public void PacketShouldContainMessage()
        {
            for (var i = 0; i < messageCount; i++)
            {
                AssertAreSameFromOffsets(this.instance.message(i), 0, this.instance.packet(i), AckSystem.NOTIFY_HEADER_SIZE, this.instance.message(i).Length);
            }
        }
    }
}

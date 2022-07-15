using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.AckSystemTests
{
    /// <summary>
    /// Send is done in setup, and then tests just valid that the sent data is correct
    /// </summary>
    [Category("SocketLayer")]
    public class AckSystemTest_Notify_1stSend : AckSystemTestBase
    {
        private SubIRawConnection connection;
        private AckSystem ackSystem;

        /// <summary>
        /// Bytes given to ack system
        /// </summary>
        private byte[] message;

        /// <summary>
        /// Bytes out of ack system
        /// </summary>
        private byte[] packet;
        private ushort maxSequence;

        [SetUp]
        public void SetUp()
        {
            var config = new Config();
            this.maxSequence = (ushort)((1 << config.SequenceSize) - 1);

            this.connection = new SubIRawConnection();
            this.ackSystem = new AckSystem(this.connection, config, MAX_PACKET_SIZE, new Time(), this.bufferPool);

            this.message = this.createRandomData(1);
            this.ackSystem.SendNotify(this.message);

            // should have got 1 packet
            Assert.That(this.connection.packets.Count, Is.EqualTo(1));
            this.packet = this.connection.packets[0];

            // should have sent data
            Assert.That(this.packet, Is.Not.Null);
        }

        [Test]
        public void PacketShouldBeNotify()
        {
            var offset = 0;
            var packetType = ByteUtils.ReadByte(this.packet, ref offset);
            Assert.That((PacketType)packetType, Is.EqualTo(PacketType.Notify));
        }

        [Test]
        public void SentSequenceShouldBe1()
        {
            var offset = 1;
            var sequance = ByteUtils.ReadUShort(this.packet, ref offset);
            Assert.That(sequance, Is.EqualTo(0));
        }

        [Test]
        public void LatestReceivedShouldBeMax()
        {
            var offset = 3;
            var received = ByteUtils.ReadUShort(this.packet, ref offset);
            Assert.That(received, Is.EqualTo(this.maxSequence));
        }
        [Test]
        public void ReceivedMaskShouldBe0()
        {
            var offset = 5;
            var mask = ByteUtils.ReadUShort(this.packet, ref offset);
            Assert.That(mask, Is.EqualTo(0));
        }

        [Test]
        public void PacketShouldContainMessage()
        {
            AssertAreSameFromOffsets(this.message, 0, this.packet, AckSystem.NOTIFY_HEADER_SIZE, this.message.Length);
        }
    }
}

using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.AckSystemTests
{
    /// <summary>
    /// Send is done in setup, and then tests just valid that the sent data is correct
    /// </summary>
    [Category("SocketLayer")]
    public class AckSystemTest_Fragmentation_Receive : AckSystemTestBase
    {
        private AckSystem ackSystem;
        private Config config;
        private byte[] message;
        private byte[] packet1;
        private byte[] packet2;

        [SetUp]
        public void SetUp()
        {
            this.config = new Config();
            var mtu = MAX_PACKET_SIZE;
            var bigSize = (int)(mtu * 1.5f);

            this.message = this.CreateBigData(1, bigSize);

            var sender = new AckTestInstance();
            sender.connection = new SubIRawConnection();
            sender.ackSystem = new AckSystem(sender.connection, this.config, MAX_PACKET_SIZE, new Time(), this.bufferPool);
            sender.ackSystem.SendReliable(this.message);
            this.packet1 = sender.packet(0);
            this.packet2 = sender.packet(1);


            var connection = new SubIRawConnection();
            this.ackSystem = new AckSystem(connection, this.config, MAX_PACKET_SIZE, new Time(), this.bufferPool);
        }

        private byte[] CreateBigData(int id, int size)
        {
            var buffer = new byte[size];
            this.rand.NextBytes(buffer);
            buffer[0] = (byte)id;

            return buffer;
        }


        [Test]
        [TestCase(-2, ExpectedResult = false)]
        [TestCase(-1, ExpectedResult = false)]
        [TestCase(0, ExpectedResult = true, Description = "equal to max is invalid")]
        [TestCase(1, ExpectedResult = true)]
        [TestCase(2, ExpectedResult = true)]
        [TestCase(5, ExpectedResult = true)]
        public bool ShouldBeInvalidIfFragmentIsOverMax(int differenceToMax)
        {
            var max = this.config.MaxReliableFragments;
            var badPacket = new byte[AckSystem.MIN_RELIABLE_FRAGMENT_HEADER_SIZE];
            var offset = 0;
            // write as if it is normal packet
            ByteUtils.WriteByte(badPacket, ref offset, 0);
            ByteUtils.WriteUShort(badPacket, ref offset, 0);
            ByteUtils.WriteUShort(badPacket, ref offset, 0);
            ByteUtils.WriteULong(badPacket, ref offset, 0);
            ByteUtils.WriteUShort(badPacket, ref offset, 0);
            // write bad index (over max)
            var fragment = max + differenceToMax;
            ByteUtils.WriteByte(badPacket, ref offset, (byte)fragment);

            return this.ackSystem.InvalidFragment(badPacket);
        }


        [Test]
        public void MessageShouldBeInQueueAfterReceive()
        {
            this.ackSystem.ReceiveReliable(this.packet1, this.packet1.Length, true);

            Assert.IsFalse(this.ackSystem.NextReliablePacket(out var _));

            this.ackSystem.ReceiveReliable(this.packet2, this.packet2.Length, true);

            var bytesIn1 = MAX_PACKET_SIZE - AckSystem.MIN_RELIABLE_FRAGMENT_HEADER_SIZE;
            var bytesIn2 = this.message.Length - bytesIn1;

            Assert.IsTrue(this.ackSystem.NextReliablePacket(out var first));

            Assert.IsTrue(first.isFragment);
            Assert.That(first.buffer.array[0], Is.EqualTo(1), "First fragment should have index 1");
            Assert.That(first.length, Is.EqualTo(bytesIn1 + 1));
            AssertAreSameFromOffsets(this.message, 0, first.buffer.array, 1, bytesIn1);

            var second = this.ackSystem.GetNextFragment();
            Assert.IsTrue(second.isFragment);
            Assert.That(second.buffer.array[0], Is.EqualTo(0), "Second fragment should have index 0");
            Assert.That(second.length, Is.EqualTo(bytesIn2 + 1));
            AssertAreSameFromOffsets(this.message, bytesIn1, second.buffer.array, 1, bytesIn2);
        }
    }
}

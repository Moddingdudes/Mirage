using System.Collections.Generic;
using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.AckSystemTests
{
    /// <summary>
    /// Send is done in setup, and then tests just valid that the sent data is correct
    /// </summary>
    [Category("SocketLayer")]
    public class AckSystemTest_Fragmentation_Send : AckSystemTestBase
    {
        private AckTestInstance instance;

        [SetUp]
        public void SetUp()
        {
            var config = new Config();
            var mtu = MAX_PACKET_SIZE;
            var bigSize = (int)(mtu * 1.5f);

            var message = this.CreateBigData(1, bigSize);

            this.instance = new AckTestInstance();
            this.instance.connection = new SubIRawConnection();
            this.instance.ackSystem = new AckSystem(this.instance.connection, config, MAX_PACKET_SIZE, new Time(), this.bufferPool);

            // create and send n messages
            this.instance.messages = new List<byte[]>();
            this.instance.messages.Add(message);
            this.instance.ackSystem.SendReliable(message);

            // should not have null data
            Assert.That(this.instance.connection.packets, Does.Not.Contain(null));
        }

        private byte[] CreateBigData(int id, int size)
        {
            var buffer = new byte[size];
            this.rand.NextBytes(buffer);
            buffer[0] = (byte)id;

            return buffer;
        }

        [Test]
        public void ShouldHaveSent2Packets()
        {
            Assert.That(this.instance.connection.packets.Count, Is.EqualTo(2));
        }

        [Test]
        public void MessageShouldBeReliableFragment()
        {
            foreach (var packet in this.instance.connection.packets)
            {
                var offset = 0;
                var packetType = ByteUtils.ReadByte(packet, ref offset);
                Assert.That((PacketType)packetType, Is.EqualTo(PacketType.ReliableFragment));
            }
        }

        [Test]
        public void EachPacketHasDifferentAckSequence()
        {
            for (var i = 0; i < this.instance.connection.packets.Count; i++)
            {
                var offset = 1;
                var sequance = ByteUtils.ReadUShort(this.instance.packet(i), ref offset);
                Assert.That(sequance, Is.EqualTo(i));
            }
        }

        [Test]
        public void EachPacketHasDifferentReliableOrder()
        {
            for (var i = 0; i < this.instance.connection.packets.Count; i++)
            {
                var offset = 1 + 2 + 2 + 8;
                var reliableOrder = ByteUtils.ReadUShort(this.instance.packet(i), ref offset);

                Assert.That(reliableOrder, Is.EqualTo(i));
            }
        }

        [Test]
        public void EachPacketHasDifferentFragmentIndex()
        {
            for (var i = 0; i < this.instance.connection.packets.Count; i++)
            {
                var offset = 1 + 2 + 2 + 8 + 2;
                ushort fragmentIndex = ByteUtils.ReadByte(this.instance.packet(i), ref offset);
                Assert.That(fragmentIndex, Is.EqualTo(1 - i), "Should be reverse Index, first packet should have 1 and second should have 0");
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.AckSystemTests
{
    /// <summary>
    /// Send is done in setup, and then tests just valid that the sent data is correct
    /// </summary>
    [Category("SocketLayer")]
    public class AckSystemTest_Notify_DroppedSends : AckSystemTestBase
    {
        private const int messageCount = 5;
        private ushort maxSequence;
        private AckTestInstance instance1;
        private AckTestInstance instance2;

        // what message get received each instance
        private bool[] received1 = new bool[messageCount] {
            true,
            false,
            false,
            true,
            true,
        };
        private bool[] received2 = new bool[messageCount] {
            false,
            true,
            true,
            false,
            true,
        };

        [SetUp]
        public void SetUp()
        {
            var config = new Config();
            this.maxSequence = (ushort)((1 << config.SequenceSize) - 1);

            this.instance1 = new AckTestInstance();
            this.instance1.connection = new SubIRawConnection();
            this.instance1.ackSystem = new AckSystem(this.instance1.connection, config, MAX_PACKET_SIZE, new Time(), this.bufferPool);


            this.instance2 = new AckTestInstance();
            this.instance2.connection = new SubIRawConnection();
            this.instance2.ackSystem = new AckSystem(this.instance2.connection, config, MAX_PACKET_SIZE, new Time(), this.bufferPool);

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

                // give to instance2 if received
                if (this.received2[i])
                    this.instance2.ackSystem.ReceiveNotify(this.instance1.connection.packets[i], this.instance1.connection.packets[i].Length);

                // send to conn2
                this.instance2.ackSystem.SendNotify(this.instance2.messages[i]);
                // give to instance1 if received
                if (this.received1[i])
                    this.instance1.ackSystem.ReceiveNotify(this.instance2.connection.packets[i], this.instance2.connection.packets[i].Length);
            }

            // should have got 1 packet
            Assert.That(this.instance1.connection.packets.Count, Is.EqualTo(messageCount));
            Assert.That(this.instance2.connection.packets.Count, Is.EqualTo(messageCount));

            // should not have null data
            Assert.That(this.instance1.connection.packets, Does.Not.Contain(null));
            Assert.That(this.instance2.connection.packets, Does.Not.Contain(null));
        }


        [Test]
        public void ReceivedShouldBeEqualToLatest()
        {
            var nextReceive = this.maxSequence;
            for (var i = 0; i < messageCount; i++)
            {
                var offset = 3;
                var received = ByteUtils.ReadUShort(this.instance1.packet(i), ref offset);
                Assert.That(received, Is.EqualTo(nextReceive), "Received should start at max and increment each time");

                // do at end becuase 1 is sending first
                if (this.received1[i])
                    nextReceive = (ushort)(i);
            }

            nextReceive = this.maxSequence;
            for (var i = 0; i < messageCount; i++)
            {
                // do at start becuase 2 is sending second
                if (this.received2[i])
                    nextReceive = (ushort)(i);

                var offset = 3;
                var received = ByteUtils.ReadUShort(this.instance2.packet(i), ref offset);
                Assert.That(received, Is.EqualTo(nextReceive), "Received should start at 1 (received first message before sending) and increment each time");
            }
        }

        [Test]
        public void MaskShouldBePreviousSequences()
        {
            var expectedMask1 = new uint[5] {
                0b0,    // no received
                0b1,    // i=0 received
                0b1,    // still just i=0
                0b1,    // still just i=0
                0b1001, // received i=3
            };
            var expectedMask2 = new uint[5] {
                0b0,    // i=0 not received
                0b1,    // i=1 received
                0b11,   // i=2 received
                0b11,   // still just i=2
                0b1101, // received i=4
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
                Assert.That(mask[i], Is.EqualTo(expectedMask1[i]), $"Received should contain previous receives\n  instance 1, index{i}\n{string.Join(",", mask.Select(x => x.ToString()))}");
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
                Assert.That(mask[i], Is.EqualTo(expectedMask2[i]), $"Received should contain previous receives\n  instance 2, index{i}\n{string.Join(",", mask.Select(x => x.ToString()))}");
            }
        }
    }
}

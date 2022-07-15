using Mirage.Serialization;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Serialization.StructMessages
{
    public struct SomeStructMessage
    {
        public int someValue;
    }

    [TestFixture]
    public class StructMessagesTests
    {
        private readonly NetworkWriter writer = new NetworkWriter(1300);
        private readonly NetworkReader reader = new NetworkReader();

        [TearDown]
        public void TearDown()
        {
            this.writer.Reset();
            this.reader.Dispose();
        }

        [Test]
        public void SerializeAreAddedWhenEmptyInStruct()
        {
            this.writer.Reset();

            const int someValue = 3;
            this.writer.Write(new SomeStructMessage
            {
                someValue = someValue,
            });

            this.reader.Reset(this.writer.ToArraySegment());
            var received = this.reader.Read<SomeStructMessage>();

            Assert.AreEqual(someValue, received.someValue);

            var writeLength = this.writer.ByteLength;
            var readLength = this.reader.BytePosition;
            Assert.That(writeLength == readLength, $"OnSerializeAll and OnDeserializeAll calls write the same amount of data\n    writeLength={writeLength}\n    readLength={readLength}");
        }
    }
}

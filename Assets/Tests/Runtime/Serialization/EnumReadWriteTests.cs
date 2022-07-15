using Mirage.Serialization;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Serialization
{
    public static class MyCustomEnumReadWrite
    {
        public static void WriteMyCustomEnum(this NetworkWriter networkWriter, EnumReadWriteTests.MyCustom customEnum)
        {
            // if O write N
            if (customEnum == EnumReadWriteTests.MyCustom.O)
            {
                networkWriter.WriteInt32((int)EnumReadWriteTests.MyCustom.N);
            }
            else
            {
                networkWriter.WriteInt32((int)customEnum);
            }
        }
        public static EnumReadWriteTests.MyCustom ReadMyCustomEnum(this NetworkReader networkReader)
        {
            return (EnumReadWriteTests.MyCustom)networkReader.ReadInt32();
        }
    }
    public class EnumReadWriteTests
    {
        public enum MyByte : byte
        {
            A, B, C, D
        }

        public enum MyShort : short
        {
            E, F, G, H
        }

        public enum MyCustom
        {
            M, N, O, P
        }

        private readonly NetworkWriter writer = new NetworkWriter(1300);
        private readonly NetworkReader reader = new NetworkReader();

        [TearDown]
        public void TearDown()
        {
            this.writer.Reset();
            this.reader.Dispose();
        }

        [Test]
        public void ByteIsSentForByteEnum()
        {
            var byteEnum = MyByte.B;

            this.writer.Write(byteEnum);

            // should only be 1 byte
            Assert.That(this.writer.ByteLength, Is.EqualTo(1));
        }

        [Test]
        public void ShortIsSentForShortEnum()
        {
            var shortEnum = MyShort.G;

            this.writer.Write(shortEnum);

            // should only be 1 byte
            Assert.That(this.writer.ByteLength, Is.EqualTo(2));
        }

        [Test]
        public void CustomWriterIsUsedForEnum()
        {
            var customEnum = MyCustom.O;
            var clientMsg = this.SerializeAndDeserializeMessage(customEnum);

            // custom writer should write N if it sees O
            Assert.That(clientMsg, Is.EqualTo(MyCustom.N));
        }

        private T SerializeAndDeserializeMessage<T>(T msg)
        {
            this.writer.Write(msg);

            this.reader.Reset(this.writer.ToArraySegment());
            return this.reader.Read<T>();
        }
    }
}

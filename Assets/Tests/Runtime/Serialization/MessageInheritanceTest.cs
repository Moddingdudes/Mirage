using Mirage.Serialization;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Serialization
{
    public class ParentMessage
    {
        public int parentValue;
    }

    public class ChildMessage : ParentMessage
    {
        public int childValue;
    }


    public abstract class RequestMessageBase
    {
        public int responseId;
    }
    public class ResponseMessage : RequestMessageBase
    {
        public int state;
        public string message = "";
        public int errorCode; // optional for error codes
    }

    //reverseOrder to test this https://github.com/vis2k/Mirror/issues/1925
    public class ResponseMessageReverse : RequestMessageBaseReverse
    {
        public int state;
        public string message = "";
        public int errorCode; // optional for error codes
    }
    public abstract class RequestMessageBaseReverse
    {
        public int responseId;
    }

    [TestFixture]
    public class MessageInheritanceTest
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
        public void SendsVauesInParentAndChildClass()
        {
            this.writer.Write(new ChildMessage
            {
                parentValue = 3,
                childValue = 4
            });

            this.reader.Reset(this.writer.ToArraySegment());
            var received = this.reader.Read<ChildMessage>();

            Assert.AreEqual(3, received.parentValue);
            Assert.AreEqual(4, received.childValue);

            var writeLength = this.writer.ByteLength;
            var readLength = this.reader.BytePosition;
            Assert.That(writeLength == readLength, $"OnSerializeAll and OnDeserializeAll calls write the same amount of data\n    writeLength={writeLength}\n    readLength={readLength}");
        }

        [Test]
        public void SendsVauesWhenUsingAbstractClass()
        {
            const int state = 2;
            const string message = "hello world";
            const int responseId = 5;
            this.writer.Write(new ResponseMessage
            {
                state = state,
                message = message,
                responseId = responseId,
            });

            this.reader.Reset(this.writer.ToArraySegment());

            var received = this.reader.Read<ResponseMessage>();

            Assert.AreEqual(state, received.state);
            Assert.AreEqual(message, received.message);
            Assert.AreEqual(responseId, received.responseId);

            var writeLength = this.writer.ByteLength;
            var readLength = this.reader.BytePosition;
            Assert.That(writeLength == readLength, $"OnSerializeAll and OnDeserializeAll calls write the same amount of data\n    writeLength={writeLength}\n    readLength={readLength}");
        }

        [Test]
        public void SendsVauesWhenUsingAbstractClassReverseDefineOrder()
        {
            const int state = 2;
            const string message = "hello world";
            const int responseId = 5;
            this.writer.Write(new ResponseMessageReverse
            {
                state = state,
                message = message,
                responseId = responseId,
            });

            this.reader.Reset(this.writer.ToArraySegment());

            var received = this.reader.Read<ResponseMessageReverse>();

            Assert.AreEqual(state, received.state);
            Assert.AreEqual(message, received.message);
            Assert.AreEqual(responseId, received.responseId);

            var writeLength = this.writer.ByteLength;
            var readLength = this.reader.BytePosition;
            Assert.That(writeLength == readLength, $"OnSerializeAll and OnDeserializeAll calls write the same amount of data\n    writeLength={writeLength}\n    readLength={readLength}");
        }
    }
}

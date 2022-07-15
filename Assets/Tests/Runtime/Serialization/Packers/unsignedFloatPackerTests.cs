using Mirage.Serialization;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Serialization.Packers
{
    public class UnsignedFloatPackerTests : PackerTestBase
    {
        private FloatPacker packer;
        private float max;
        private float precsion;

        [SetUp]
        public void Setup()
        {
            this.max = 100;
            this.precsion = 1 / 1000f;
            this.packer = new FloatPacker(this.max, this.precsion, false);
        }

        [Test]
        public void ClampsToZero()
        {
            this.packer.Pack(this.writer, -4.5f);
            var outValue = this.packer.Unpack(this.GetReader());

            Assert.That(outValue, Is.Zero);
        }

        [Test]
        public void CanWriteNearMax()
        {
            const float value = 99.5f;
            this.packer.Pack(this.writer, value);
            var outValue = this.packer.Unpack(this.GetReader());

            Assert.That(outValue, Is.EqualTo(value).Within(this.precsion));
        }
    }
}

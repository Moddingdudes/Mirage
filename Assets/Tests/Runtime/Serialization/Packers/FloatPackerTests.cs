using Mirage.Serialization;
using NUnit.Framework;
using Random = UnityEngine.Random;

namespace Mirage.Tests.Runtime.Serialization.Packers
{
    [TestFixture(100, 0.1f, true)]
    [TestFixture(500, 0.02f, true)]
    [TestFixture(2000, 0.05f, true)]
    [TestFixture(1.5f, 0.01f, true)]
    [TestFixture(100_000, 30, true)]

    [TestFixture(100, 0.1f, false)]
    [TestFixture(500, 0.02f, false)]
    [TestFixture(2000, 0.05f, false)]
    [TestFixture(1.5f, 0.01f, false)]
    [TestFixture(100_000, 30, false)]
    public class FloatPackerTests : PackerTestBase
    {
        private readonly FloatPacker packer;
        private readonly float max;
        private readonly float min;
        private readonly float precsion;
        private readonly bool signed;

        public FloatPackerTests(float max, float precsion, bool signed)
        {
            this.max = max;
            this.min = signed ? -max : 0;
            this.precsion = precsion;
            this.signed = signed;
            this.packer = new FloatPacker(max, precsion, signed);
        }

        private float GetRandomFloat()
        {
            return Random.Range(this.min, this.max);
        }


        [Test]
        // takes about 1 seconds per 1000 values (including all fixtures)
        [Repeat(1000)]
        public void UnpackedValueIsWithinPrecision()
        {
            var start = this.GetRandomFloat();
            var packed = this.packer.Pack(start);
            var unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }

        [Test]
        public void ValueOverMaxWillBeUnpackedAsMax()
        {
            var start = this.max * 1.2f;
            var packed = this.packer.Pack(start);
            var unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(this.max).Within(this.precsion));
        }

        [Test]
        public void ValueUnderNegativeMaxWillBeUnpackedAsNegativeMax()
        {
            var start = this.max * -1.2f;
            var packed = this.packer.Pack(start);
            var unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(this.min).Within(this.precsion));
        }

        [Test]
        public void ZeroUnpackToExactlyZero()
        {
            const float zero = 0;
            var packed = this.packer.Pack(zero);
            var unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(zero));
        }


        [Test]
        [Repeat(100)]
        public void UnpackedValueIsWithinPrecisionUsingWriter()
        {
            var start = this.GetRandomFloat();
            this.packer.Pack(this.writer, start);
            var unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }

        [Test]
        public void ValueOverMaxWillBeUnpackedUsingWriterAsMax()
        {
            var start = this.max * 1.2f;
            this.packer.Pack(this.writer, start);
            var unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(this.max).Within(this.precsion));
        }

        [Test]
        public void ValueUnderNegativeMaxWillBeUnpackedUsingWriterAsNegativeMax()
        {
            var start = this.max * -1.2f;
            this.packer.Pack(this.writer, start);
            var unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(this.min).Within(this.precsion));
        }


        [Test]
        [Repeat(100)]
        public void UnpackedValueIsWithinPrecisionNoClamp()
        {
            var start = this.GetRandomFloat();
            var packed = this.packer.PackNoClamp(start);
            var unpacked = this.packer.Unpack(packed);

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }

        [Test]
        [Repeat(100)]
        public void UnpackedValueIsWithinPrecisionNoClampUsingWriter()
        {
            var start = this.GetRandomFloat();
            this.packer.PackNoClamp(this.writer, start);
            var unpacked = this.packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start).Within(this.precsion));
        }
    }
}

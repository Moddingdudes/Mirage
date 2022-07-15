using System;
using Mirage.Serialization;
using NUnit.Framework;
using Random = System.Random;

namespace Mirage.Tests.Runtime.Serialization.Packers
{
    [TestFixture(4)]
    [TestFixture(7)]
    [TestFixture(8)]
    [TestFixture(12)]
    [TestFixture(16)]
    public class UintBlockPackerTests : PackerTestBase
    {
        private readonly Random random = new Random();
        private readonly int blockSize;

        public UintBlockPackerTests(int blockSize)
        {
            this.blockSize = blockSize;
        }

        private ulong GetRandonUlongBias()
        {
            return (ulong)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * ulong.MaxValue);
        }

        private uint GetRandonUintBias()
        {
            return (uint)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * uint.MaxValue);
        }

        private ushort GetRandonUshortBias()
        {
            return (ushort)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * ushort.MaxValue);
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUlongValue()
        {
            var start = this.GetRandonUlongBias();
            VarIntBlocksPacker.Pack(this.writer, start, this.blockSize);
            var unpacked = VarIntBlocksPacker.Unpack(this.GetReader(), this.blockSize);

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUintValue()
        {
            var start = this.GetRandonUintBias();
            VarIntBlocksPacker.Pack(this.writer, start, this.blockSize);
            var unpacked = VarIntBlocksPacker.Unpack(this.GetReader(), this.blockSize);

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUshortValue()
        {
            var start = this.GetRandonUshortBias();
            VarIntBlocksPacker.Pack(this.writer, start, this.blockSize);
            var unpacked = VarIntBlocksPacker.Unpack(this.GetReader(), this.blockSize);

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        public void WritesNplus1BitsPerBlock()
        {
            var zero = 0u;
            VarIntBlocksPacker.Pack(this.writer, zero, this.blockSize);
            Assert.That(this.writer.BitPosition, Is.EqualTo(this.blockSize + 1));

            var unpacked = VarIntBlocksPacker.Unpack(this.GetReader(), this.blockSize);
            Assert.That(unpacked, Is.EqualTo(zero));
        }

        [Test]
        public void WritesNplus1BitsPerBlock_bigger()
        {
            var aboveBlockSize = (1u << this.blockSize) + 1u;
            VarIntBlocksPacker.Pack(this.writer, aboveBlockSize, this.blockSize);
            Assert.That(this.writer.BitPosition, Is.EqualTo(2 * (this.blockSize + 1)));

            var unpacked = VarIntBlocksPacker.Unpack(this.GetReader(), this.blockSize);
            Assert.That(unpacked, Is.EqualTo(aboveBlockSize));
        }
    }
}

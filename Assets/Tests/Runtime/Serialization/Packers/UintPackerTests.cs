using System;
using Mirage.Serialization;
using NUnit.Framework;
using Random = System.Random;

namespace Mirage.Tests.Runtime.Serialization.Packers
{
    [TestFixture(50ul, 1_000ul, null)]
    [TestFixture(250ul, 10_000ul, null)]
    [TestFixture(500ul, 100_000ul, null)]
    [TestFixture(50ul, 1_000ul, 10_000_000ul)]
    [TestFixture(250ul, 10_000ul, 10_000_000ul)]
    [TestFixture(500ul, 100_000ul, 10_000_000ul)]
    public class UintPackerTests : PackerTestBase
    {
        private readonly Random random = new Random();
        private readonly VarIntPacker packer;
        private readonly ulong max;

        public UintPackerTests(ulong smallValue, ulong mediumValue, ulong? largeValue)
        {
            if (largeValue.HasValue)
            {
                this.packer = new VarIntPacker(smallValue, mediumValue, largeValue.Value, false);
                this.max = largeValue.Value;
            }
            else
            {
                this.packer = new VarIntPacker(smallValue, mediumValue);
                this.max = ulong.MaxValue;
            }
        }

        private ulong GetRandonUlongBias()
        {
            return (ulong)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * this.max);
        }

        private uint GetRandonUintBias()
        {
            return (uint)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * Math.Min(this.max, uint.MaxValue));
        }

        private ushort GetRandonUshortBias()
        {
            return (ushort)(Math.Abs(this.random.NextDouble() - this.random.NextDouble()) * Math.Min(this.max, ushort.MaxValue));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUlongValue()
        {
            var start = this.GetRandonUlongBias();
            this.packer.PackUlong(this.writer, start);
            var unpacked = this.packer.UnpackUlong(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUintValue()
        {
            var start = this.GetRandonUintBias();
            this.packer.PackUint(this.writer, start);
            var unpacked = this.packer.UnpackUint(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start));
        }

        [Test]
        [Repeat(1000)]
        public void UnpacksCorrectUshortValue()
        {
            var start = this.GetRandonUshortBias();
            this.packer.PackUshort(this.writer, start);
            var unpacked = this.packer.UnpackUshort(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(start));
        }
    }
}

using Mirage.Serialization;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Serialization.Packers
{
    public class PackerTestBase
    {
        public readonly NetworkWriter writer = new NetworkWriter(1300);
        private readonly NetworkReader reader = new NetworkReader();

        [TearDown]
        public virtual void TearDown()
        {
            this.writer.Reset();
            this.reader.Dispose();
        }

        /// <summary>
        /// Gets Reader using the current data inside writer
        /// </summary>
        /// <returns></returns>
        public NetworkReader GetReader()
        {
            this.reader.Reset(this.writer.ToArraySegment());
            return this.reader;
        }
    }
}

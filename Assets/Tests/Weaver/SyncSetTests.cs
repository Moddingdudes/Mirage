using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    // Some tests for SyncObjects are in SyncListTests and apply to SyncDictionary too
    public class SyncSetTests : TestsBuildFromTestName
    {
        [Test]
        public void SyncSetByteValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncSetGenericAbstractInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncSetGenericInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncSetInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncSetStruct()
        {
            this.IsSuccess();
        }
    }
}

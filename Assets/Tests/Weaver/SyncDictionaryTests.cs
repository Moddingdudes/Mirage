using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    // Some tests for SyncObjects are in SyncListTests and apply to SyncDictionary too
    public class SyncDictionaryTests : TestsBuildFromTestName
    {
        [Test]
        public void SyncDictionary()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryGenericAbstractInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryGenericInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryStructKey()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryStructItem()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryErrorForGenericStructKey()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryErrorForGenericStructItem()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncDictionaryErrorWhenUsingGenericInNetworkBehaviour()
        {
            this.IsSuccess();
        }
    }
}

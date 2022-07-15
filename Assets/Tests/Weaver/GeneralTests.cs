using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class GeneralTests : TestsBuildFromTestName
    {
        [Test]
        public void RecursionCount()
        {
            this.IsSuccess();
        }

        [Test]
        public void TestingScriptableObjectArraySerialization()
        {
            this.IsSuccess();
        }
    }
}

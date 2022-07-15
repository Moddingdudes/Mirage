using Mirage.Tests.Runtime.Host;
using NUnit.Framework;

namespace Mirage.Experimental.Tests.Host
{
    [TestFixture]
    public class NetworkTransformTest : HostSetup<NetworkTransform>
    {
        [Test]
        public void InitexcludeOwnerUpdateTest()
        {
            Assert.That(this.component.excludeOwnerUpdate, Is.True);
        }

        [Test]
        public void InitsyncPositionTest()
        {
            Assert.That(this.component.syncPosition, Is.True);
        }

        [Test]
        public void InitsyncRotationTest()
        {
            Assert.That(this.component.syncRotation, Is.True);
        }

        [Test]
        public void InitsyncScaleTest()
        {
            Assert.That(this.component.syncScale, Is.True);
        }

        [Test]
        public void InitinterpolatePositionTest()
        {
            Assert.That(this.component.interpolatePosition, Is.True);
        }

        [Test]
        public void InitinterpolateRotationTest()
        {
            Assert.That(this.component.interpolateRotation, Is.True);
        }

        [Test]
        public void InitinterpolateScaleTest()
        {
            Assert.That(this.component.interpolateScale, Is.True);
        }

        [Test]
        public void InitlocalPositionSensitivityTest()
        {
            Assert.That(this.component.localPositionSensitivity, Is.InRange(0.001f, 0.199f));
        }

        [Test]
        public void InitlocalRotationSensitivityTest()
        {
            Assert.That(this.component.localRotationSensitivity, Is.InRange(0.001f, 0.199f));
        }

        [Test]
        public void InitlocalScaleSensitivityTest()
        {
            Assert.That(this.component.localScaleSensitivity, Is.InRange(0.001f, 0.199f));
        }
    }
}

using Mirage.Experimental;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class NetworkRigidbodyTest : HostSetup<NetworkRigidbody>
    {
        [Test]
        public void InitsyncVelocityTest()
        {
            Assert.That(this.component.syncVelocity, Is.True);
        }

        [Test]
        public void InitvelocitySensitivityTest()
        {
            Assert.That(this.component.velocitySensitivity, Is.InRange(0.001f, 0.199f));
        }

        [Test]
        public void InitsyncAngularVelocityTest()
        {
            Assert.That(this.component.syncAngularVelocity, Is.True);
        }

        [Test]
        public void InitangularVelocitySensitivityTest()
        {
            Assert.That(this.component.angularVelocitySensitivity, Is.InRange(0.001f, 0.199f));
        }
    }
}

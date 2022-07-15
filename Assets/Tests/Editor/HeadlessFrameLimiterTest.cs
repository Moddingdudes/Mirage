using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mirage.Tests
{
    [TestFixture]
    public class HeadlessFrameLimiterTest : MonoBehaviour
    {
        protected GameObject testGO;
        protected HeadlessFrameLimiter comp;

        [SetUp]
        public void Setup()
        {
            this.testGO = new GameObject();
            this.comp = this.testGO.AddComponent<HeadlessFrameLimiter>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(this.testGO);
        }

        [Test]
        public void StartOnHeadlessValue()
        {
            Assert.That(this.comp.serverTickRate, Is.EqualTo(30));
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mirage.Tests
{
    [TestFixture]
    public class HeadlessAutoStartTest : MonoBehaviour
    {
        protected GameObject testGO;
        protected HeadlessAutoStart comp;

        [SetUp]
        public void Setup()
        {
            this.testGO = new GameObject();
            this.comp = this.testGO.AddComponent<HeadlessAutoStart>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(this.testGO);
        }

        [Test]
        public void StartOnHeadlessValue()
        {
            Assert.That(this.comp.startOnHeadless, Is.True);
        }
    }
}

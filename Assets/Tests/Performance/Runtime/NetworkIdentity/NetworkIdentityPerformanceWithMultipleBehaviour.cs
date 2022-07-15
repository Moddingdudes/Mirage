using NSubstitute;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Mirage.Tests.Performance
{
    [Category("Performance")]
    [Category("Benchmark")]
    public class NetworkIdentityPerformanceWithMultipleBehaviour
    {
        private const int healthCount = 32;
        private GameObject gameObject;
        private NetworkIdentity identity;
        private Health[] health;


        [SetUp]
        public void SetUp()
        {
            this.gameObject = new GameObject();
            this.identity = this.gameObject.AddComponent<NetworkIdentity>();
            this.identity.Owner = Substitute.For<INetworkPlayer>();
            this.identity.observers.Add(this.identity.Owner);
            this.health = new Health[healthCount];
            for (var i = 0; i < healthCount; i++)
            {
                this.health[i] = this.gameObject.AddComponent<Health>();
                this.health[i].syncMode = SyncMode.Owner;
                this.health[i].syncInterval = 0f;
            }
        }
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(this.gameObject);
        }

        [Test]
        [Performance]
        public void ServerUpdateIsDirty()
        {
            Measure.Method(this.RunServerUpdateIsDirty)
                .WarmupCount(10)
                .MeasurementCount(100)
                .Run();
        }

        private void RunServerUpdateIsDirty()
        {
            for (var j = 0; j < 1000; j++)
            {
                for (var i = 0; i < healthCount; i++)
                {
                    this.health[i].SetDirtyBit(1UL);
                }
                this.identity.UpdateVars();
            }
        }

        [Test]
        [Performance]
        public void ServerUpdateNotDirty()
        {
            Measure.Method(this.RunServerUpdateNotDirty)
                .WarmupCount(10)
                .MeasurementCount(100)
                .Run();
        }

        private void RunServerUpdateNotDirty()
        {
            for (var j = 0; j < 1000; j++)
            {
                this.identity.UpdateVars();
            }
        }
    }
}


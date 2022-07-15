using NSubstitute;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Mirage.Tests.Performance
{
    public class Health : NetworkBehaviour
    {
        [SyncVar] public int health = 10;

        public void Update()
        {
            this.health = (this.health + 1) % 10;
        }
    }
    [Category("Performance")]
    [Category("Benchmark")]
    public class NetworkIdentityPerformance
    {
        private GameObject gameObject;
        private NetworkIdentity identity;
        private Health health;


        [SetUp]
        public void SetUp()
        {
            this.gameObject = new GameObject();
            this.identity = this.gameObject.AddComponent<NetworkIdentity>();
            this.identity.Owner = Substitute.For<INetworkPlayer>();
            this.identity.observers.Add(this.identity.Owner);
            this.health = this.gameObject.AddComponent<Health>();
            this.health.syncMode = SyncMode.Owner;
            this.health.syncInterval = 0f;
        }
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(this.gameObject);
        }

        [Test]
        [Performance]
        public void NetworkIdentityServerUpdateIsDirty()
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
                this.health.SetDirtyBit(1UL);
                this.identity.UpdateVars();
            }
        }

        [Test]
        [Performance]
        public void NetworkIdentityServerUpdateNotDirty()
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


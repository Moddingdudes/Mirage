using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Mirage.Tests.Performance
{
    [Category("Performance")]
    [Category("Benchmark")]
    public class NetworkIdentitySpawningPerformance
    {
        private List<GameObject> spawned = new List<GameObject>();
        private GameObject prefab;

        private GameObject Spawn()
        {
            var clone = GameObject.Instantiate(this.prefab);
            this.spawned.Add(clone);
            clone.SetActive(true);
            return clone;
        }

        [SetUp]
        public void SetUp()
        {
            this.prefab = new GameObject("NetworkPrefab");
            this.prefab.SetActive(false); // disable so that NetworkIdentity.Awake is not called
            this.prefab.AddComponent<NetworkIdentity>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(this.prefab);
            foreach (var item in this.spawned)
                UnityEngine.Object.DestroyImmediate(item);

            this.spawned.Clear();
        }

        [Test]
        [Performance]
        public void SpawnManyObjects()
        {
            Debug.Log($"Debug build:{Debug.isDebugBuild}");
            Measure.Method(() =>
            {
                // spawn 100 objects
                for (var i = 0; i < 100; i++)
                {
                    _ = this.Spawn();
                }
            })
                .WarmupCount(10)
                .MeasurementCount(100)
                .Run();
        }
    }
}


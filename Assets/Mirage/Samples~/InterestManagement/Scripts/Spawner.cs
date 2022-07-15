using UnityEngine;

namespace Mirage.Examples.InterestManagement
{
    public class Spawner : MonoBehaviour
    {
        public int count = 50;
        public Bounds bounds;

        public NetworkIdentity prefab;
        public ServerObjectManager serverObjectManager;

        public void Spawn()
        {
            for (var i = 0; i < this.count; i++)
                this.SpawnPrefab();
        }

        public void SpawnPrefab()
        {
            var position = new Vector3(
                (Random.value - 0.5f) * this.bounds.size.x + this.bounds.center.x,
                (Random.value - 0.5f) * this.bounds.size.y + this.bounds.center.y,
                (Random.value - 0.5f) * this.bounds.size.z + this.bounds.center.z
            );

            var newLoot = GameObject.Instantiate(this.prefab, position, Quaternion.identity, this.transform);

            this.serverObjectManager.Spawn(newLoot);
        }
    }
}
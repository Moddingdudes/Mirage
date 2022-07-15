using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Mirage.Examples.InterestManagement
{
    public class Wander : MonoBehaviour
    {
        public NavMeshAgent agent;

        public Bounds bounds;

        // Start is called before the first frame update
        public void StartMoving()
        {
            this.StartCoroutine(this.Move());
        }

        public IEnumerator Move()
        {
            while (true)
            {

                var position = new Vector3(
                    (Random.value - 0.5f) * this.bounds.size.x + this.bounds.center.x,
                    (Random.value - 0.5f) * this.bounds.size.y + this.bounds.center.y,
                    (Random.value - 0.5f) * this.bounds.size.z + this.bounds.center.z
                );

                this.agent.destination = position;

                yield return new WaitForSeconds(Random.Range(1.0f, 5.0f));
            }
        }
    }
}

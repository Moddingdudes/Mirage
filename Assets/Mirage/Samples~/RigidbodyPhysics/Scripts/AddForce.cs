using UnityEngine;

namespace Mirage.Examples.RigidbodyPhysics
{
    public class AddForce : NetworkBehaviour
    {
        [SerializeField] private float force = 500f;

        private void Update()
        {
            if (this.IsServer && Input.GetKeyDown(KeyCode.Space))
            {
                this.GetComponent<Rigidbody>().AddForce(Vector3.up * this.force);
            }
        }
    }
}

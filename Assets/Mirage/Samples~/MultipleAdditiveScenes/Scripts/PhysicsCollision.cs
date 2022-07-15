using UnityEngine;

namespace Mirage.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsCollision : NetworkBehaviour
    {
        [Tooltip("how forcefully to push this object")]
        public float force = 12;

        public Rigidbody rigidbody3D;

        private void OnValidate()
        {
            if (this.rigidbody3D == null)
                this.rigidbody3D = this.GetComponent<Rigidbody>();
        }

        private void Start()
        {
            this.rigidbody3D.isKinematic = !this.IsServer;
        }

        [Server(error = false)]
        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // get direction from which player is contacting object
                var direction = other.contacts[0].normal;

                // zero the y and normalize so we don't shove this through the floor or launch this over the wall
                direction.y = 0;
                direction = direction.normalized;

                // push this away from player...a bit less force for host player
                if (other.gameObject.GetComponent<NetworkIdentity>().Owner == this.Server.LocalPlayer)
                    this.rigidbody3D.AddForce(direction * this.force * .5f);
                else
                    this.rigidbody3D.AddForce(direction * this.force);
            }
        }
    }
}

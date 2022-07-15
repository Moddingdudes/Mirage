using UnityEngine;

namespace Mirage.Examples.Additive
{
    // This script demonstrates the NetworkAnimator and how to leverage
    // the built-in observers system to track players.
    // Note that all ProximityCheckers should be restricted to the Player layer.
    public class ShootingTankBehaviour : NetworkBehaviour
    {
        [SyncVar]
        public Quaternion rotation;
        private NetworkAnimator networkAnimator;

        [Server(error = false)]
        private void Start()
        {
            this.networkAnimator = this.GetComponent<NetworkAnimator>();
        }

        [Range(0, 1)]
        public float turnSpeed = 0.1f;

        private void Update()
        {
            if (this.IsServer && this.Identity.observers.Count > 0)
                this.ShootNearestPlayer();

            if (this.IsClient)
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, this.rotation, this.turnSpeed);
        }

        [Server]
        private void ShootNearestPlayer()
        {
            GameObject target = null;
            var distance = 100f;

            foreach (var networkConnection in this.Identity.observers)
            {
                var tempTarget = networkConnection.Identity.gameObject;
                var tempDistance = Vector3.Distance(tempTarget.transform.position, this.transform.position);

                if (target == null || distance > tempDistance)
                {
                    target = tempTarget;
                    distance = tempDistance;
                }
            }

            if (target != null)
            {
                this.transform.LookAt(target.transform.position + Vector3.down);
                this.rotation = this.transform.rotation;
                this.networkAnimator.SetTrigger("Fire");
            }
        }
    }
}

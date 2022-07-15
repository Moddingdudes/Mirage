using UnityEngine;

namespace Mirage.Experimental
{
    [AddComponentMenu("Network/Experimental/NetworkLerpRigidbody")]
    [HelpURL("https://miragenet.github.io/Mirage/Articles/Components/NetworkLerpRigidbody.html")]
    public class NetworkLerpRigidbody : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] internal Rigidbody target;

        [Tooltip("How quickly current velocity approaches target velocity")]
        public float lerpVelocityAmount = 0.5f;

        [Tooltip("How quickly current position approaches target position")]
        public float lerpPositionAmount = 0.5f;

        [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
        public bool clientAuthority;
        private float nextSyncTime;


        [SyncVar]
        private Vector3 targetVelocity;

        [SyncVar]
        private Vector3 targetPosition;

        /// <summary>
        /// Ignore value if is host or client with Authority
        /// </summary>
        /// <returns></returns>
        private bool IgnoreSync => this.IsServer || this.ClientWithAuthority;

        private bool ClientWithAuthority => this.clientAuthority && this.HasAuthority;

        private void OnValidate()
        {
            if (this.target == null)
            {
                this.target = this.GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            if (this.IsServer)
            {
                this.SyncToClients();
            }
            else if (this.ClientWithAuthority)
            {
                this.SendToServer();
            }
        }

        private void SyncToClients()
        {
            this.targetVelocity = this.target.velocity;
            this.targetPosition = this.target.position;
        }

        private void SendToServer()
        {
            var now = Time.time;
            if (now > this.nextSyncTime)
            {
                this.nextSyncTime = now + this.syncInterval;
                this.CmdSendState(this.target.velocity, this.target.position);
            }
        }

        [ServerRpc]
        private void CmdSendState(Vector3 velocity, Vector3 position)
        {
            this.target.velocity = velocity;
            this.target.position = position;
            this.targetVelocity = velocity;
            this.targetPosition = position;
        }

        private void FixedUpdate()
        {
            if (this.IgnoreSync) { return; }

            this.target.velocity = Vector3.Lerp(this.target.velocity, this.targetVelocity, this.lerpVelocityAmount);
            this.target.position = Vector3.Lerp(this.target.position, this.targetPosition, this.lerpPositionAmount);
            // add velocity to position as position would have moved on server at that velocity
            this.targetPosition += this.target.velocity * Time.fixedDeltaTime;

            // TODO does this also need to sync acceleration so and update velocity?
        }
    }
}

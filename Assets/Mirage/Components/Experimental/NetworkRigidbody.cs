using Mirage.Logging;
using UnityEngine;

namespace Mirage.Experimental
{
    [AddComponentMenu("Network/Experimental/NetworkRigidbody")]
    [HelpURL("https://miragenet.github.io/Mirage/Articles/Components/NetworkRigidbody.html")]
    public class NetworkRigidbody : NetworkBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkRigidbody));

        [Header("Settings")]
        public Rigidbody target;

        [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
        public bool clientAuthority;

        [Header("Velocity")]

        [Tooltip("Syncs Velocity every SyncInterval")]
        public bool syncVelocity = true;

        [Tooltip("Set velocity to 0 each frame (only works if syncVelocity is false")]
        public bool clearVelocity;

        [Tooltip("Only Syncs Value if distance between previous and current is great than sensitivity")]
        public float velocitySensitivity = 0.1f;

        [Header("Angular Velocity")]

        [Tooltip("Syncs AngularVelocity every SyncInterval")]
        public bool syncAngularVelocity = true;

        [Tooltip("Set angularVelocity to 0 each frame (only works if syncAngularVelocity is false")]
        public bool clearAngularVelocity;

        [Tooltip("Only Syncs Value if distance between previous and current is great than sensitivity")]
        public float angularVelocitySensitivity = 0.1f;

        /// <summary>
        /// Values sent on client with authority after they are sent to the server
        /// </summary>
        private readonly ClientSyncState previousValue = new ClientSyncState();

        private void OnValidate()
        {
            if (this.target == null)
            {
                this.target = this.GetComponent<Rigidbody>();
            }
        }

        #region Sync vars
        [SyncVar(hook = nameof(OnVelocityChanged))]
        private Vector3 velocity;

        [SyncVar(hook = nameof(OnAngularVelocityChanged))]
        private Vector3 angularVelocity;

        [SyncVar(hook = nameof(OnIsKinematicChanged))]
        private bool isKinematic;

        [SyncVar(hook = nameof(OnUseGravityChanged))]
        private bool useGravity;

        [SyncVar(hook = nameof(OnuDragChanged))]
        private float drag;

        [SyncVar(hook = nameof(OnAngularDragChanged))]
        private float angularDrag;

        /// <summary>
        /// Ignore value if is host or client with Authority
        /// </summary>
        /// <returns></returns>
        private bool IgnoreSync => this.IsServer || this.ClientWithAuthority;

        private bool ClientWithAuthority => this.clientAuthority && this.HasAuthority;

        private void OnVelocityChanged(Vector3 _, Vector3 newValue)
        {
            if (this.IgnoreSync)
                return;

            this.target.velocity = newValue;
        }

        private void OnAngularVelocityChanged(Vector3 _, Vector3 newValue)
        {
            if (this.IgnoreSync)
                return;

            this.target.angularVelocity = newValue;
        }

        private void OnIsKinematicChanged(bool _, bool newValue)
        {
            if (this.IgnoreSync)
                return;

            this.target.isKinematic = newValue;
        }

        private void OnUseGravityChanged(bool _, bool newValue)
        {
            if (this.IgnoreSync)
                return;

            this.target.useGravity = newValue;
        }

        private void OnuDragChanged(float _, float newValue)
        {
            if (this.IgnoreSync)
                return;

            this.target.drag = newValue;
        }

        private void OnAngularDragChanged(float _, float newValue)
        {
            if (this.IgnoreSync)
                return;

            this.target.angularDrag = newValue;
        }
        #endregion


        internal void Update()
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

        internal void FixedUpdate()
        {
            if (this.clearAngularVelocity && !this.syncAngularVelocity)
            {
                this.target.angularVelocity = Vector3.zero;
            }

            if (this.clearVelocity && !this.syncVelocity)
            {
                this.target.velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Updates sync var values on server so that they sync to the client
        /// </summary>
        [Server]
        private void SyncToClients()
        {
            // only update if they have changed more than Sensitivity

            var currentVelocity = this.syncVelocity ? this.target.velocity : default;
            var currentAngularVelocity = this.syncAngularVelocity ? this.target.angularVelocity : default;

            var velocityChanged = this.syncVelocity && ((this.previousValue.velocity - currentVelocity).sqrMagnitude > this.velocitySensitivity * this.velocitySensitivity);
            var angularVelocityChanged = this.syncAngularVelocity && ((this.previousValue.angularVelocity - currentAngularVelocity).sqrMagnitude > this.angularVelocitySensitivity * this.angularVelocitySensitivity);

            if (velocityChanged)
            {
                this.velocity = currentVelocity;
                this.previousValue.velocity = currentVelocity;
            }

            if (angularVelocityChanged)
            {
                this.angularVelocity = currentAngularVelocity;
                this.previousValue.angularVelocity = currentAngularVelocity;
            }

            // other rigidbody settings
            this.isKinematic = this.target.isKinematic;
            this.useGravity = this.target.useGravity;
            this.drag = this.target.drag;
            this.angularDrag = this.target.angularDrag;
        }

        /// <summary>
        /// Uses ServerRpc to send values to server
        /// </summary>
        [Client]
        private void SendToServer()
        {
            if (!this.HasAuthority)
            {
                logger.LogWarning("SendToServer called without authority");
                return;
            }

            this.SendVelocity();
            this.SendRigidBodySettings();
        }

        [Client]
        private void SendVelocity()
        {
            var now = Time.time;
            if (now < this.previousValue.nextSyncTime)
                return;

            var currentVelocity = this.syncVelocity ? this.target.velocity : default;
            var currentAngularVelocity = this.syncAngularVelocity ? this.target.angularVelocity : default;

            var velocityChanged = this.syncVelocity && ((this.previousValue.velocity - currentVelocity).sqrMagnitude > this.velocitySensitivity * this.velocitySensitivity);
            var angularVelocityChanged = this.syncAngularVelocity && ((this.previousValue.angularVelocity - currentAngularVelocity).sqrMagnitude > this.angularVelocitySensitivity * this.angularVelocitySensitivity);

            // if angularVelocity has changed it is likely that velocity has also changed so just sync both values
            // however if only velocity has changed just send velocity
            if (angularVelocityChanged)
            {
                this.CmdSendVelocityAndAngular(currentVelocity, currentAngularVelocity);
                this.previousValue.velocity = currentVelocity;
                this.previousValue.angularVelocity = currentAngularVelocity;
            }
            else if (velocityChanged)
            {
                this.CmdSendVelocity(currentVelocity);
                this.previousValue.velocity = currentVelocity;
            }


            // only update syncTime if either has changed
            if (angularVelocityChanged || velocityChanged)
            {
                this.previousValue.nextSyncTime = now + this.syncInterval;
            }
        }

        [Client]
        private void SendRigidBodySettings()
        {
            // These shouldn't change often so it is ok to send in their own ServerRpc
            if (this.previousValue.isKinematic != this.target.isKinematic)
            {
                this.CmdSendIsKinematic(this.target.isKinematic);
                this.previousValue.isKinematic = this.target.isKinematic;
            }
            if (this.previousValue.useGravity != this.target.useGravity)
            {
                this.CmdSendUseGravity(this.target.useGravity);
                this.previousValue.useGravity = this.target.useGravity;
            }
            if (this.previousValue.drag != this.target.drag)
            {
                this.CmdSendDrag(this.target.drag);
                this.previousValue.drag = this.target.drag;
            }
            if (this.previousValue.angularDrag != this.target.angularDrag)
            {
                this.CmdSendAngularDrag(this.target.angularDrag);
                this.previousValue.angularDrag = this.target.angularDrag;
            }
        }

        /// <summary>
        /// Called when only Velocity has changed on the client
        /// </summary>
        [ServerRpc]
        private void CmdSendVelocity(Vector3 velocity)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            this.velocity = velocity;
            this.target.velocity = velocity;
        }

        /// <summary>
        /// Called when angularVelocity has changed on the client
        /// </summary>
        [ServerRpc]
        private void CmdSendVelocityAndAngular(Vector3 velocity, Vector3 angularVelocity)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            if (this.syncVelocity)
            {
                this.velocity = velocity;

                this.target.velocity = velocity;

            }
            this.angularVelocity = angularVelocity;
            this.target.angularVelocity = angularVelocity;
        }

        [ServerRpc]
        private void CmdSendIsKinematic(bool isKinematic)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            this.isKinematic = isKinematic;
            this.target.isKinematic = isKinematic;
        }

        [ServerRpc]
        private void CmdSendUseGravity(bool useGravity)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            this.useGravity = useGravity;
            this.target.useGravity = useGravity;
        }

        [ServerRpc]
        private void CmdSendDrag(float drag)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            this.drag = drag;
            this.target.drag = drag;
        }

        [ServerRpc]
        private void CmdSendAngularDrag(float angularDrag)
        {
            // Ignore messages from client if not in client authority mode
            if (!this.clientAuthority)
                return;

            this.angularDrag = angularDrag;
            this.target.angularDrag = angularDrag;
        }

        /// <summary>
        /// holds previously synced values
        /// </summary>
        public class ClientSyncState
        {
            /// <summary>
            /// Next sync time that velocity will be synced, based on syncInterval.
            /// </summary>
            public float nextSyncTime;
            public Vector3 velocity;
            public Vector3 angularVelocity;
            public bool isKinematic;
            public bool useGravity;
            public float drag;
            public float angularDrag;
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

namespace Mirage.Examples.Tanks
{
    public class Tank : NetworkBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator animator;

        [Header("Movement")]
        public float rotationSpeed = 100;

        [Header("Firing")]
        public KeyCode shootKey = KeyCode.Space;
        public GameObject projectilePrefab;
        public Transform projectileMount;

        [Header("Game Stats")]
        [SyncVar]
        public int health;
        [SyncVar]
        public int score;
        [SyncVar]
        public string playerName;
        [SyncVar]
        public bool allowMovement;
        [SyncVar]
        public bool isReady;

        public bool IsDead => this.health <= 0;
        public TextMesh nameText;

        private void Update()
        {
            if (Camera.main)
            {
                this.nameText.text = this.playerName;
                this.nameText.transform.rotation = Camera.main.transform.rotation;
            }

            // movement for local player
            if (!this.IsLocalPlayer)
                return;

            //Set local players name color to green
            this.nameText.color = Color.green;

            if (!this.allowMovement)
                return;

            if (this.IsDead)
                return;

            // rotate
            var horizontal = Input.GetAxis("Horizontal");
            this.transform.Rotate(0, horizontal * this.rotationSpeed * Time.deltaTime, 0);

            // move
            var vertical = Input.GetAxis("Vertical");
            var forward = this.transform.TransformDirection(Vector3.forward);
            this.agent.velocity = forward * Mathf.Max(vertical, 0) * this.agent.speed;
            this.animator.SetBool("Moving", this.agent.velocity != Vector3.zero);

            // shoot
            if (Input.GetKeyDown(this.shootKey))
            {
                this.CmdFire();
            }
        }

        // this is called on the server
        [ServerRpc]
        private void CmdFire()
        {
            var projectile = Instantiate(this.projectilePrefab, this.projectileMount.position, this.transform.rotation);
            projectile.GetComponent<Projectile>().source = this.gameObject;
            this.ServerObjectManager.Spawn(projectile);
            this.RpcOnFire();
        }

        // this is called on the tank that fired for all observers
        [ClientRpc]
        private void RpcOnFire()
        {
            this.animator.SetTrigger("Shoot");
        }

        public void SendReadyToServer(string playername)
        {
            if (!this.IsLocalPlayer)
                return;

            this.CmdReady(playername);
        }

        [ServerRpc]
        private void CmdReady(string playername)
        {
            if (string.IsNullOrEmpty(playername))
            {
                this.playerName = "PLAYER" + Random.Range(1, 99);
            }
            else
            {
                this.playerName = playername;
            }

            this.isReady = true;
        }
    }
}

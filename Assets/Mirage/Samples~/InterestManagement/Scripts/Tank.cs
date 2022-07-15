using UnityEngine;
using UnityEngine.AI;

namespace Mirage.Examples.InterestManagement
{
    public class Tank : NetworkBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator animator;

        [Header("Movement")]
        public float rotationSpeed = 100;

        [Header("Game Stats")]
        [SyncVar]
        public string playerName;

        public TextMesh nameText;

        [Server]
        public void SetRandomName()
        {
            this.playerName = "PLAYER" + Random.Range(1, 99);
        }

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

            // rotate
            var horizontal = Input.GetAxis("Horizontal");
            this.transform.Rotate(0, horizontal * this.rotationSpeed * Time.deltaTime, 0);

            // move
            var vertical = Input.GetAxis("Vertical");
            var forward = this.transform.TransformDirection(Vector3.forward);
            this.agent.velocity = forward * Mathf.Max(vertical, 0) * this.agent.speed;
            this.animator.SetBool("Moving", this.agent.velocity != Vector3.zero);

        }

    }
}

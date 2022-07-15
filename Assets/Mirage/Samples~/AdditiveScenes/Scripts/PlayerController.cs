using UnityEngine;

namespace Mirage.Examples.Additive
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkBehaviour
    {
        public CharacterController characterController;
        public CapsuleCollider capsuleCollider;

        private void Awake()
        {
            this.Identity.OnStartLocalPlayer.AddListener(this.OnStartLocalPlayer);
        }

        private void OnValidate()
        {
            if (this.characterController == null)
                this.characterController = this.GetComponent<CharacterController>();
            if (this.capsuleCollider == null)
                this.capsuleCollider = this.GetComponent<CapsuleCollider>();
        }

        private void Start()
        {
            this.capsuleCollider.enabled = this.IsServer;
        }

        public void OnStartLocalPlayer()
        {
            this.characterController.enabled = true;

            Camera.main.orthographic = false;
            Camera.main.transform.SetParent(this.transform);
            Camera.main.transform.localPosition = new Vector3(0f, 3f, -8f);
            Camera.main.transform.localEulerAngles = new Vector3(10f, 0f, 0f);
        }

        private void OnDisable()
        {
            if (this.IsLocalPlayer && Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.transform.SetParent(null);
                Camera.main.transform.localPosition = new Vector3(0f, 70f, 0f);
                Camera.main.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            }
        }

        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float turnSensitivity = 5f;
        public float maxTurnSpeed = 150f;

        [Header("Diagnostics")]
        public float horizontal;
        public float vertical;
        public float turn;
        public float jumpSpeed;
        public bool isGrounded = true;
        public bool isFalling;
        public Vector3 velocity;

        private void Update()
        {
            if (!this.IsLocalPlayer || !this.characterController.enabled)
                return;

            this.horizontal = Input.GetAxis("Horizontal");
            this.vertical = Input.GetAxis("Vertical");

            // Q and E cancel each other out, reducing the turn to zero
            if (Input.GetKey(KeyCode.Q))
                this.turn = Mathf.MoveTowards(this.turn, -this.maxTurnSpeed, this.turnSensitivity);
            if (Input.GetKey(KeyCode.E))
                this.turn = Mathf.MoveTowards(this.turn, this.maxTurnSpeed, this.turnSensitivity);
            if (Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.E))
                this.turn = Mathf.MoveTowards(this.turn, 0, this.turnSensitivity);
            if (!Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.E))
                this.turn = Mathf.MoveTowards(this.turn, 0, this.turnSensitivity);

            if (this.isGrounded)
                this.isFalling = false;

            if ((this.isGrounded || !this.isFalling) && this.jumpSpeed < 1f && Input.GetKey(KeyCode.Space))
            {
                this.jumpSpeed = Mathf.Lerp(this.jumpSpeed, 1f, 0.5f);
            }
            else if (!this.isGrounded)
            {
                this.isFalling = true;
                this.jumpSpeed = 0;
            }
        }

        private void FixedUpdate()
        {
            if (!this.IsLocalPlayer || this.characterController == null)
                return;

            this.transform.Rotate(0f, this.turn * Time.fixedDeltaTime, 0f);

            var direction = new Vector3(this.horizontal, this.jumpSpeed, this.vertical);
            direction = Vector3.ClampMagnitude(direction, 1f);
            direction = this.transform.TransformDirection(direction);
            direction *= this.moveSpeed;

            if (this.jumpSpeed > 0)
                this.characterController.Move(direction * Time.fixedDeltaTime);
            else
                this.characterController.SimpleMove(direction);

            this.isGrounded = this.characterController.isGrounded;
            this.velocity = this.characterController.velocity;
        }
    }
}

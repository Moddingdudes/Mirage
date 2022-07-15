using UnityEngine;

namespace Mirage.Examples.Pong
{
    public class Ball : NetworkBehaviour
    {
        public float speed = 30;
        public Rigidbody2D rigidbody2d;

        private void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.OnStartServer);
        }

        public void OnStartServer()
        {
            // only simulate ball physics on server
            this.rigidbody2d.simulated = true;

            // Serve the ball from left player
            this.rigidbody2d.velocity = Vector2.right * this.speed;
        }

        private float HitFactor(Vector2 ballPos, Vector2 racketPos, float racketHeight)
        {
            // ascii art:
            // ||  1 <- at the top of the racket
            // ||
            // ||  0 <- at the middle of the racket
            // ||
            // || -1 <- at the bottom of the racket
            return (ballPos.y - racketPos.y) / racketHeight;
        }

        // only call this on server
        [Server(error = false)]
        private void OnCollisionEnter2D(Collision2D col)
        {
            // Note: 'col' holds the collision information. If the
            // Ball collided with a racket, then:
            //   col.gameObject is the racket
            //   col.transform.position is the racket's position
            //   col.collider is the racket's collider

            // did we hit a racket? then we need to calculate the hit factor
            if (col.transform.GetComponent<Player>())
            {
                // Calculate y direction via hit Factor
                var y = this.HitFactor(this.transform.position,
                                    col.transform.position,
                                    col.collider.bounds.size.y);

                // Calculate x direction via opposite collision
                float x = col.relativeVelocity.x > 0 ? 1 : -1;

                // Calculate direction, make length=1 via .normalized
                var dir = new Vector2(x, y).normalized;

                // Set Velocity with dir * speed
                this.rigidbody2d.velocity = dir * this.speed;
            }
        }
    }
}

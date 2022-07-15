using UnityEngine;

namespace Mirage.Examples.Tanks
{
    public class Projectile : NetworkBehaviour
    {
        public float destroyAfter = 1;
        public Rigidbody rigidBody;
        public float force = 1000;

        private void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.OnStartServer);
        }

        [Header("Game Stats")]
        public int damage;
        public GameObject source;

        public void OnStartServer()
        {
            this.Invoke(nameof(DestroySelf), this.destroyAfter);
        }

        // set velocity for server and client. this way we don't have to sync the
        // position, because both the server and the client simulate it.
        private void Start()
        {
            this.rigidBody.AddForce(this.transform.forward * this.force);
        }

        // destroy for everyone on the server
        [Server]
        private void DestroySelf()
        {
            this.ServerObjectManager.Destroy(this.gameObject);
        }

        // [Server] because we don't want a warning if OnTriggerEnter is
        // called on the client
        [Server(error = false)]
        private void OnTriggerEnter(Collider co)
        {
            //Hit another player
            if (co.tag.Equals("Player") && co.gameObject != this.source)
            {
                //Apply damage
                co.GetComponent<Tank>().health -= this.damage;

                //update score on source
                this.source.GetComponent<Tank>().score += this.damage;
            }

            this.ServerObjectManager.Destroy(this.gameObject);
        }
    }
}

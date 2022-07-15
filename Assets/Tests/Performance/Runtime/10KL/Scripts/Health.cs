using System.Collections;
using UnityEngine;

namespace Mirage.Examples.Light
{
    public class Health : NetworkBehaviour
    {
        [SyncVar] public int health = 10;
        private void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.OnStartServer);
            this.Identity.OnStopServer.AddListener(this.OnStopServer);
        }

        public void OnStartServer()
        {
            this.StartCoroutine(this.UpdateHealth());
        }

        public void OnStopServer()
        {
            this.StopAllCoroutines();
        }

        internal IEnumerator UpdateHealth()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(0f, 5f));
                this.health = (this.health + 1) % 10;
            }
        }
    }
}

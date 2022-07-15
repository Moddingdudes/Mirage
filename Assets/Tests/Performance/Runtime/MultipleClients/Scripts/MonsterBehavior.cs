using System.Collections;
using UnityEngine;

namespace Mirage.Tests.Performance.Runtime
{
    public class MonsterBehavior : NetworkBehaviour
    {
        [SyncVar]
        public Vector3 position;

        [SyncVar]
        public int MonsterId;

        public void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.StartServer);
            this.Identity.OnStopServer.AddListener(this.StopServer);
        }

        private void StopServer()
        {
            this.StopAllCoroutines();
        }

        private void StartServer()
        {
            this.StartCoroutine(this.MoveMonster());
        }

        private IEnumerator MoveMonster()
        {
            while (true)
            {
                this.position = Random.insideUnitSphere;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
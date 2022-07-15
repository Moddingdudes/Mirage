using System.Collections;
using UnityEngine;

namespace Mirage.HeadlessBenchmark
{
    public class AdjustableLoad : NetworkBehaviour
    {
        private float movementDuration = 5.0f;
        private float waitBeforeMoving = 5.0f;
        private bool hasArrived = false;

        private void Update()
        {
            if (!this.IsLocalPlayer)
                return;

            if (!this.hasArrived)
            {
                this.hasArrived = true;
                var randX = Random.Range(-15.0f, 15.0f);
                var randY = Random.Range(-15.0f, 15.0f);
                this.StartCoroutine(this.MoveToPoint(new Vector3(randX, randY, 0)));
            }
        }

        private IEnumerator MoveToPoint(Vector3 targetPos)
        {
            var timer = 0.0f;
            var startPos = this.transform.position;

            while (timer < this.movementDuration)
            {
                timer += Time.deltaTime;
                var t = timer / this.movementDuration;
                t = t * t * t * (t * (6f * t - 15f) + 10f);
                this.transform.position = Vector3.Lerp(startPos, targetPos, t);

                yield return null;
            }

            yield return new WaitForSeconds(this.waitBeforeMoving);
            this.hasArrived = false;
        }
    }
}

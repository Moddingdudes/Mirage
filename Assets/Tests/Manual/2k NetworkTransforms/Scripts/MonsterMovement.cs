using UnityEngine;

namespace Mirage.Examples.OneK
{
    public class MonsterMovement : NetworkBehaviour
    {
        public float speed = 1;
        public float movementProbability = 0.5f;
        public float movementDistance = 20;
        private bool moving;
        private Vector3 start;
        private Vector3 destination;

        public void OnStartServer()
        {
            this.start = this.transform.position;
        }

        [Server(error = false)]
        private void Update()
        {
            if (this.moving)
            {
                if (Vector3.Distance(this.transform.position, this.destination) <= 0.01f)
                {
                    this.moving = false;
                }
                else
                {
                    this.transform.position = Vector3.MoveTowards(this.transform.position, this.destination, this.speed * Time.deltaTime);
                }
            }
            else
            {
                var r = Random.value;
                if (r < this.movementProbability * Time.deltaTime)
                {
                    var circlePos = Random.insideUnitCircle;
                    var dir = new Vector3(circlePos.x, 0, circlePos.y);
                    var dest = this.transform.position + dir * this.movementDistance;

                    // within move dist around start?
                    // (don't want to wander off)
                    if (Vector3.Distance(this.start, dest) <= this.movementDistance)
                    {
                        this.destination = dest;
                        this.moving = true;
                    }
                }
            }
        }
    }
}

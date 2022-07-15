using Mirage.Logging;
using UnityEngine;

namespace Mirage.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(RandomColor))]
    public class Reward : NetworkBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Reward));

        public bool available = true;
        public Spawner spawner;
        public RandomColor randomColor;

        private void OnValidate()
        {
            if (this.randomColor == null)
                this.randomColor = this.GetComponent<RandomColor>();
        }

        [Server(error = false)]
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                this.ClaimPrize(other.gameObject);
            }
        }

        // This is called from PlayerController.CmdClaimPrize which is invoked by PlayerController.OnControllerColliderHit
        // This only runs on the server
        public void ClaimPrize(GameObject player)
        {
            if (this.available)
            {
                // This is a fast switch to prevent two players claiming the prize in a bang-bang close contest for it.
                // First hit turns it off, pending the object being destroyed a few frames later.
                this.available = false;

                Color prizeColor = this.randomColor.color;

                // calculate the points from the color ... lighter scores higher as the average approaches 255
                // UnityEngine.Color RGB values are float fractions of 255
                var points = (uint)(((prizeColor.r * 255) + (prizeColor.g * 255) + (prizeColor.b * 255)) / 3);
                if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "Scored {0} points R:{1} G:{2} B:{3}", points, prizeColor.r, prizeColor.g, prizeColor.b);

                // award the points via SyncVar on the PlayerController
                player.GetComponent<PlayerScore>().score += points;

                // spawn a replacement
                this.spawner.SpawnPrize();

                // destroy this one
                this.ServerObjectManager.Destroy(this.gameObject);
            }
        }
    }
}

using UnityEngine;

namespace Mirage.Examples.MultipleAdditiveScenes
{
    public class PlayerScore : NetworkBehaviour
    {
        [SyncVar]
        public int playerNumber;

        [SyncVar]
        public int scoreIndex;

        [SyncVar]
        public int matchIndex;

        [SyncVar]
        public uint score;

        public int clientMatchIndex = -1;

        private void OnGUI()
        {
            if (!this.IsLocalPlayer && this.clientMatchIndex < 0)
                this.clientMatchIndex = this.Client.Player.Identity.GetComponent<PlayerScore>().matchIndex;

            if (this.IsLocalPlayer || this.matchIndex == this.clientMatchIndex)
                GUI.Box(new Rect(10f + (this.scoreIndex * 110), 10f, 100f, 25f), $"P{this.playerNumber}: {this.score}");
        }
    }
}

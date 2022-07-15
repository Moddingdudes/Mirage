using UnityEngine;
using UnityEngine.UI;

namespace Mirage.Examples.Basic
{
    public class Player : NetworkBehaviour
    {
        [Header("Player Components")]
        public RectTransform rectTransform;
        public Image image;

        [Header("Child Text Objects")]
        public Text playerNameText;
        public Text playerDataText;

        // These are set in OnStartServer and used in OnStartClient
        [SyncVar]
        private int playerNo;
        [SyncVar]
        private Color playerColor;

        private static int playerCounter = 1;

        private static int GetNextPlayerId()
        {
            return playerCounter++;
        }

        // This is updated by UpdateData which is called from OnStartServer via InvokeRepeating
        [SyncVar(hook = nameof(OnPlayerDataChanged))]
        public int playerData;

        private void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.OnStartServer);
            this.Identity.OnStartClient.AddListener(this.OnStartClient);
            this.Identity.OnStartLocalPlayer.AddListener(this.OnStartLocalPlayer);
        }

        // This is called by the hook of playerData SyncVar above
        private void OnPlayerDataChanged(int oldPlayerData, int newPlayerData)
        {
            // Show the data in the UI
            this.playerDataText.text = string.Format("Data: {0:000}", newPlayerData);
        }

        // This fires on server when this player object is network-ready
        public void OnStartServer()
        {
            // Set SyncVar values
            this.playerNo = GetNextPlayerId();
            this.playerColor = Random.ColorHSV(0f, 1f, 0.9f, 0.9f, 1f, 1f);

            // Start generating updates
            this.InvokeRepeating(nameof(UpdateData), 1, 1);
        }

        // This only runs on the server, called from OnStartServer via InvokeRepeating
        [Server(error = false)]
        private void UpdateData()
        {
            this.playerData = Random.Range(100, 1000);
        }

        // This fires on all clients when this player object is network-ready
        public void OnStartClient()
        {
            // Calculate position in the layout panel
            var x = 100 + ((this.playerNo % 4) * 150);
            var y = -170 - ((this.playerNo / 4) * 80);
            this.rectTransform.anchoredPosition = new Vector2(x, y);

            // Apply SyncVar values
            this.playerNameText.color = this.playerColor;
            this.playerNameText.text = string.Format("Player {0:00}", this.playerNo);
        }

        // This only fires on the local client when this player object is network-ready
        public void OnStartLocalPlayer()
        {
            // apply a shaded background to our player
            this.image.color = new Color(1f, 1f, 1f, 0.1f);
        }
    }
}

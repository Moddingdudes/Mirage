using System.Collections;
using Mirage.Logging;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Mirage.Examples.Chat
{
    public class ChatWindow : MonoBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ChatWindow));

        [FormerlySerializedAs("client")]
        public NetworkClient Client;
        [FormerlySerializedAs("chatMessage")]
        public InputField ChatMessage;
        [FormerlySerializedAs("chatHistory")]
        public Text ChatHistory;
        [FormerlySerializedAs("scrollbar")]
        public Scrollbar Scrollbar;

        public void Awake()
        {
            Player.OnMessage += this.OnPlayerMessage;
        }

        public void OnDestroy()
        {
            Player.OnMessage -= this.OnPlayerMessage;
        }

        private void OnPlayerMessage(Player player, string message)
        {
            var prettyMessage = player.IsLocalPlayer ?
                $"<color=red>{player.playerName}: </color> {message}" :
                $"<color=blue>{player.playerName}: </color> {message}";
            this.AppendMessage(prettyMessage);

            logger.Log(message);
        }

        public void OnSend()
        {
            if (this.ChatMessage.text.Trim() == "")
                return;

            // get our player
            var player = this.Client.Player.Identity.GetComponent<Player>();

            // send a message
            player.CmdSend(this.ChatMessage.text.Trim());

            this.ChatMessage.text = "";
        }

        internal void AppendMessage(string message)
        {
            this.StartCoroutine(this.AppendAndScroll(message));
        }

        private IEnumerator AppendAndScroll(string message)
        {
            this.ChatHistory.text += message + "\n";

            // it takes 2 frames for the UI to update ?!?!
            yield return null;
            yield return null;

            // slam the scrollbar down
            this.Scrollbar.value = 0;
        }
    }
}

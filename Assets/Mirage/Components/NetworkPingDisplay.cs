using UnityEngine;
using UnityEngine.UI;

namespace Mirage
{
    /// <summary>
    /// Component that will display the clients ping in milliseconds
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/NetworkPingDisplay")]
    [HelpURL("https://miragenet.github.io/Mirage/Articles/Components/NetworkPingDisplay.html")]
    public class NetworkPingDisplay : MonoBehaviour
    {
        public NetworkClient Client;
        public Text NetworkPingLabelText;

        internal void Update()
        {
            if (this.Client.Active)
                this.NetworkPingLabelText.text = string.Format("{0}ms", (int)(this.Client.World.Time.Rtt * 1000));
        }
    }
}

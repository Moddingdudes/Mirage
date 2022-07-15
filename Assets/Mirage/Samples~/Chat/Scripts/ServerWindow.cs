using UnityEngine;

namespace Mirage.Examples.Chat
{

    public class ServerWindow : MonoBehaviour
    {
        public string serverIp = "localhost";

        public NetworkManager NetworkManager;

        public void StartClient()
        {
            this.NetworkManager.Client.Connect(this.serverIp);
        }

        public void StartHost()
        {
            this.NetworkManager.Server.StartServer(this.NetworkManager.Client);
        }

        public void SetServerIp(string serverIp)
        {
            this.serverIp = serverIp;
        }
    }
}

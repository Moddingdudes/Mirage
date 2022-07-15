using UnityEngine;

namespace Mirage.DisplayMetrics
{
    public class SetDisplayMetrics : MonoBehaviour
    {
        public NetworkServer server;
        public NetworkClient client;
        public DisplayMetricsAverageGui displayMetrics;

        private void Start()
        {
            if (this.server != null)
                this.server.Started.AddListener(this.ServerStarted);
            if (this.client != null)
                this.client.Connected.AddListener(this.ClientConnected);
        }

        private void ServerStarted()
        {
            this.displayMetrics.Metrics = this.server.Metrics;
        }

        private void ClientConnected(INetworkPlayer arg0)
        {
            this.displayMetrics.Metrics = this.client.Metrics;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Mirage
{
    public class NetworkManagerHud : MonoBehaviour
    {
        public NetworkManager NetworkManager;
        public string NetworkAddress = "localhost";
        public bool DontDestroy = true;

        [Header("Prefab Canvas Elements")]
        public InputField NetworkAddressInput;
        public GameObject OfflineGO;
        public GameObject OnlineGO;
        public Text StatusLabel;

        private void Start()
        {
            if (this.DontDestroy)
                DontDestroyOnLoad(this.transform.root.gameObject);

            Application.runInBackground = true;

            // return to offset menu when server or client is stopped
            this.NetworkManager.Server?.Stopped.AddListener(this.OfflineSetActive);
            this.NetworkManager.Client?.Disconnected.AddListener(_ => this.OfflineSetActive());
        }

        private void SetLabel(string value)
        {
            if (this.StatusLabel) this.StatusLabel.text = value;
        }

        internal void OnlineSetActive()
        {
            this.OfflineGO.SetActive(false);
            this.OnlineGO.SetActive(true);
        }

        internal void OfflineSetActive()
        {
            this.OfflineGO.SetActive(true);
            this.OnlineGO.SetActive(false);
        }

        public void StartHostButtonHandler()
        {
            this.SetLabel("Host Mode");
            this.NetworkManager.Server.StartServer(this.NetworkManager.Client);
            this.OnlineSetActive();
        }

        public void StartServerOnlyButtonHandler()
        {
            this.SetLabel("Server Mode");
            this.NetworkManager.Server.StartServer();
            this.OnlineSetActive();
        }

        public void StartClientButtonHandler()
        {
            this.SetLabel("Client Mode");
            this.NetworkManager.Client.Connect(this.NetworkAddress);
            this.OnlineSetActive();
        }

        public void StopButtonHandler()
        {
            this.SetLabel(string.Empty);

            if (this.NetworkManager.Server.Active)
                this.NetworkManager.Server.Stop();
            if (this.NetworkManager.Client.Active)
                this.NetworkManager.Client.Disconnect();
            this.OfflineSetActive();
        }

        public void OnNetworkAddressInputUpdate()
        {
            this.NetworkAddress = this.NetworkAddressInput.text;
        }
    }
}

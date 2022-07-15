using Mirage.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Mirage
{
    public class OnlineOfflineScene : MonoBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(OnlineOfflineScene));

        [FormerlySerializedAs("server")]
        public NetworkServer Server;

        public NetworkSceneManager NetworkSceneManager;

        [Scene]
        [Tooltip("Assign the OnlineScene to load for this zone")]
        public string OnlineScene;

        [Scene]
        [Tooltip("Assign the OfflineScene to load for this zone")]
        public string OfflineScene;

        // Start is called before the first frame update
        private void Start()
        {
            if (string.IsNullOrEmpty(this.OnlineScene))
                throw new MissingReferenceException("OnlineScene missing. Please assign to OnlineOfflineScene component.");

            if (string.IsNullOrEmpty(this.OfflineScene))
                throw new MissingReferenceException("OfflineScene missing. Please assign to OnlineOfflineScene component.");

            if (this.Server != null)
            {
                this.Server.Started.AddListener(this.OnServerStarted);
                this.Server.Stopped.AddListener(this.OnServerStopped);
            }
        }

        private void OnServerStarted()
        {
            this.NetworkSceneManager.ServerLoadSceneNormal(this.OnlineScene);
        }

        private void OnServerStopped()
        {
            Debug.Log("OnlineOfflineScene.OnServerStopped");
            this.NetworkSceneManager.ServerLoadSceneNormal(this.OfflineScene);
        }
    }
}

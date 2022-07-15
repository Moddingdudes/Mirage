using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirage.Examples.MultipleAdditiveScenes
{
    [AddComponentMenu("")]
    public class MultiSceneNetManager : NetworkManager
    {
        [Header("MultiScene Setup")]
        public int instances = 3;

        [Scene]
        public string gameScene;
        private readonly List<Scene> subScenes = new List<Scene>();

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public void Start()
        {
            this.Server.Started.AddListener(() => this.StartCoroutine(this.LoadSubScenes()));
            this.Server.Authenticated.AddListener(this.OnServerAddPlayer);
            this.Server.Stopped.AddListener(this.OnStopServer);
            this.Client.Disconnected.AddListener(this.OnStopClient);
        }

        #region Server System Callbacks

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="player">Connection from client.</param>
        public void OnServerAddPlayer(INetworkPlayer player)
        {
            // This delay is really for the host player that loads too fast for the server to have subscene loaded
            this.StartCoroutine(this.AddPlayerDelayed(player));
        }

        private int playerId = 1;

        private IEnumerator AddPlayerDelayed(INetworkPlayer player)
        {
            yield return new WaitForSeconds(.5f);

            this.ServerObjectManager.NetworkSceneManager.ServerLoadSceneAdditively(this.gameScene, this.Server.Players);

            var playerScore = player.Identity.GetComponent<PlayerScore>();
            playerScore.playerNumber = this.playerId;
            playerScore.scoreIndex = this.playerId / this.subScenes.Count;
            playerScore.matchIndex = this.playerId % this.subScenes.Count;

            if (this.subScenes.Count > 0)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player.Identity.gameObject, this.subScenes[this.playerId % this.subScenes.Count]);

            this.playerId++;
        }

        #endregion

        #region Start & Stop Callbacks



        private IEnumerator LoadSubScenes()
        {
            for (var index = 0; index < this.instances; index++)
            {
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(this.gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
                this.subScenes.Add(UnityEngine.SceneManagement.SceneManager.GetSceneAt(index + 1));
            }
        }

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public void OnStopServer()
        {
            this.Server.SendToAll(new SceneMessage { MainActivateScene = gameScene, SceneOperation = SceneOperation.UnloadAdditive });
            this.StartCoroutine(this.UnloadSubScenes());
        }

        public void OnStopClient(ClientStoppedReason reason)
        {
            if (!this.Server.Active)
                this.StartCoroutine(this.UnloadClientSubScenes());
        }

        private IEnumerator UnloadClientSubScenes()
        {
            for (var index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index) != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetSceneAt(index));
            }
        }

        private IEnumerator UnloadSubScenes()
        {
            for (var index = 0; index < this.subScenes.Count; index++)
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(this.subScenes[index]);

            this.subScenes.Clear();

            yield return Resources.UnloadUnusedAssets();
        }

        #endregion
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mirage.Examples.SceneChange
{
    public class SceneSwitcherHud : MonoBehaviour
    {
        public NetworkSceneManager sceneManager;
        public Text AdditiveButtonText;
        private bool additiveLoaded;
        private Scene _additiveLoadedScene;

        public void Update()
        {
            if (this.additiveLoaded)
            {
                this.AdditiveButtonText.text = "Additive Unload";
            }
            else
            {
                this.AdditiveButtonText.text = "Additive Load";
            }
        }

        public void Room1ButtonHandler()
        {
            this.sceneManager.ServerLoadSceneNormal("Room1");
            this.additiveLoaded = false;
        }

        public void Room2ButtonHandler()
        {
            this.sceneManager.ServerLoadSceneNormal("Room2");
            this.additiveLoaded = false;
        }

        public void AdditiveButtonHandler()
        {
            var players = this.sceneManager.Server.Players;

            if (this.additiveLoaded)
            {
                this.additiveLoaded = false;

                this.sceneManager.ServerUnloadSceneAdditively(this._additiveLoadedScene, players);
            }
            else
            {
                this.additiveLoaded = true;
                this.sceneManager.ServerLoadSceneAdditively("Additive", players);
                this._additiveLoadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            }
        }
    }
}

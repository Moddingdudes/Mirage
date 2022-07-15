using UnityEngine;

namespace Mirage.Examples.Additive
{
    public class RandomColor : NetworkBehaviour
    {
        private void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.OnStartServer);
        }

        public void OnStartServer()
        {
            this.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        }

        // Color32 packs to 4 bytes
        [SyncVar(hook = nameof(SetColor))]
        public Color32 color = Color.black;

        // Unity clones the material when GetComponent<Renderer>().material is called
        // Cache it here and destroy it in OnDestroy to prevent a memory leak
        private Material cachedMaterial;

        private void SetColor(Color32 oldColor, Color32 newColor)
        {
            if (this.cachedMaterial == null) this.cachedMaterial = this.GetComponentInChildren<Renderer>().material;
            this.cachedMaterial.color = newColor;
        }

        private void OnDestroy()
        {
            Destroy(this.cachedMaterial);
        }
    }
}

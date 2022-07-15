using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mirage
{
    [Flags]
    public enum NetworkManagerMode
    {
        None = 0,
        Server = 1,
        Client = 2,
        Host = Server | Client
    }

    [AddComponentMenu("Network/NetworkManager")]
    [HelpURL("https://miragenet.github.io/Mirage/Articles/Guides/Callbacks/NetworkManager.html")]
    [DisallowMultipleComponent]
    public class NetworkManager : MonoBehaviour
    {
        [FormerlySerializedAs("server")]
        public NetworkServer Server;
        [FormerlySerializedAs("client")]
        public NetworkClient Client;
        [FormerlySerializedAs("sceneManager")]
        [FormerlySerializedAs("SceneManager")]
        public NetworkSceneManager NetworkSceneManager;
        [FormerlySerializedAs("serverObjectManager")]
        public ServerObjectManager ServerObjectManager;
        [FormerlySerializedAs("clientObjectManager")]
        public ClientObjectManager ClientObjectManager;

        /// <summary>
        /// True if the server or client is started and running
        /// <para>This is set True in StartServer / StartClient, and set False in StopServer / StopClient</para>
        /// </summary>
        public bool IsNetworkActive => this.Server.Active || this.Client.Active;

        /// <summary>
        /// helper enum to know if we started the networkmanager as server/client/host.
        /// </summary>
        public NetworkManagerMode NetworkMode
        {
            get
            {
                if (!this.Server.Active && !this.Client.Active)
                    return NetworkManagerMode.None;
                else if (this.Server.Active && this.Client.Active)
                    return NetworkManagerMode.Host;
                else if (this.Server.Active)
                    return NetworkManagerMode.Server;
                else
                    return NetworkManagerMode.Client;
            }
        }
    }
}

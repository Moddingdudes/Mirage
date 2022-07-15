using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Mirage
{
    public class HeadlessAutoStart : MonoBehaviour
    {
        [FormerlySerializedAs("server")]
        public NetworkServer Server;

        /// <summary>
        /// Automatically invoke StartServer()
        /// <para>If the application is a Server Build or run with the -batchMode ServerRpc line argument, StartServer is automatically invoked.</para>
        /// </summary>
        [Tooltip("Should the server auto-start when the game is started in a headless build?")]
        public bool startOnHeadless = true;

        private void Start()
        {
            // headless mode? then start the server
            // can't do this in Awake because Awake is for initialization.
            // some transports might not be ready until Start.
            if (this.Server && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null && this.startOnHeadless)
            {
                this.Server.StartServer();
            }
        }
    }
}

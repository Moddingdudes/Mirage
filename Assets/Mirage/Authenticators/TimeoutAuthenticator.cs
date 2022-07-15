using System;
using System.Collections;
using System.Collections.Generic;
using Mirage.Logging;
using UnityEngine;

namespace Mirage.Authenticators
{
    /// <summary>
    /// An authenticator that disconnects connections if they don't
    /// authenticate within a specified time limit.
    /// </summary>
    [AddComponentMenu("Network/Authenticators/TimeoutAuthenticator")]
    public class TimeoutAuthenticator : NetworkAuthenticator
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(TimeoutAuthenticator));

        public NetworkAuthenticator Authenticator;

        [Range(0, 600), Tooltip("Timeout to auto-disconnect in seconds. Set to 0 for no timeout.")]
        public float Timeout = 60;

        public void Awake()
        {
            this.Authenticator.OnClientAuthenticated += this.HandleClientAuthenticated;
            this.Authenticator.OnServerAuthenticated += this.HandleServerAuthenticated;
        }

        private readonly HashSet<INetworkPlayer> pendingAuthentication = new HashSet<INetworkPlayer>();

        private void HandleServerAuthenticated(INetworkPlayer player)
        {
            this.pendingAuthentication.Remove(player);
            this.ServerAccept(player);
        }

        private void HandleClientAuthenticated(INetworkPlayer player)
        {
            this.pendingAuthentication.Remove(player);
            this.ClientAccept(player);
        }

        public override void ServerAuthenticate(INetworkPlayer player)
        {
            this.pendingAuthentication.Add(player);
            this.Authenticator.ServerAuthenticate(player);
            if (this.Timeout > 0)
                this.StartCoroutine(this.BeginAuthentication(player, this.ServerReject));
        }

        public override void ClientAuthenticate(INetworkPlayer player)
        {
            this.pendingAuthentication.Add(player);
            this.Authenticator.ClientAuthenticate(player);

            if (this.Timeout > 0)
                this.StartCoroutine(this.BeginAuthentication(player, this.ClientReject));
        }

        public override void ServerSetup(NetworkServer server)
        {
            this.Authenticator.ServerSetup(server);
        }

        public override void ClientSetup(NetworkClient client)
        {
            this.Authenticator.ClientSetup(client);
        }

        private IEnumerator BeginAuthentication(INetworkPlayer player, Action<INetworkPlayer> reject)
        {
            if (logger.LogEnabled()) logger.Log($"Authentication countdown started {player} {this.Timeout}");

            yield return new WaitForSecondsRealtime(this.Timeout);

            if (this.pendingAuthentication.Contains(player))
            {
                if (logger.LogEnabled()) logger.Log($"Authentication Timeout {player}");

                this.pendingAuthentication.Remove(player);
                reject.Invoke(player);
            }
        }
    }
}

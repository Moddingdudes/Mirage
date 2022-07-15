using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage
{
    /// <summary>
    /// Component that controls visibility of networked objects based on match id.
    /// <para>Any object with this component on it will only be visible to other objects in the same match.</para>
    /// <para>This would be used to isolate players to their respective matches within a single game server instance. </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/NetworkMatchChecker")]
    [RequireComponent(typeof(NetworkIdentity))]
    [HelpURL("https://miragenet.github.io/Mirage/Articles/Components/NetworkMatchChecker.html")]
    public class NetworkMatchChecker : NetworkVisibility
    {
        private static readonly Dictionary<Guid, HashSet<NetworkIdentity>> matchPlayers = new Dictionary<Guid, HashSet<NetworkIdentity>>();
        private Guid currentMatch = Guid.Empty;

        [Header("Diagnostics")]
        [SyncVar]
        public string currentMatchDebug;

        /// <summary>
        /// Set this to the same value on all networked objects that belong to a given match
        /// </summary>
        public Guid MatchId
        {
            get { return this.currentMatch; }
            set
            {
                if (this.currentMatch == value) return;

                // cache previous match so observers in that match can be rebuilt
                var previousMatch = this.currentMatch;

                // Set this to the new match this object just entered ...
                this.currentMatch = value;
                // ... and copy the string for the inspector because Unity can't show Guid directly
                this.currentMatchDebug = this.currentMatch.ToString();

                if (previousMatch != Guid.Empty)
                {
                    // Remove this object from the hashset of the match it just left
                    matchPlayers[previousMatch].Remove(this.Identity);

                    // RebuildObservers of all NetworkIdentity's in the match this object just left
                    this.RebuildMatchObservers(previousMatch);
                }

                if (this.currentMatch != Guid.Empty)
                {
                    // Make sure this new match is in the dictionary
                    if (!matchPlayers.ContainsKey(this.currentMatch))
                        matchPlayers.Add(this.currentMatch, new HashSet<NetworkIdentity>());

                    // Add this object to the hashset of the new match
                    matchPlayers[this.currentMatch].Add(this.Identity);

                    // RebuildObservers of all NetworkIdentity's in the match this object just entered
                    this.RebuildMatchObservers(this.currentMatch);
                }
                else
                {
                    // Not in any match now...RebuildObservers will clear and add self
                    this.Identity.RebuildObservers(false);
                }
            }
        }

        public void Awake()
        {
            this.Identity.OnStartServer.AddListener(this.OnStartServer);
        }

        public void OnStartServer()
        {
            if (this.currentMatch == Guid.Empty) return;

            if (!matchPlayers.ContainsKey(this.currentMatch))
                matchPlayers.Add(this.currentMatch, new HashSet<NetworkIdentity>());

            matchPlayers[this.currentMatch].Add(this.Identity);

            // No need to rebuild anything here.
            // identity.RebuildObservers is called right after this from NetworkServer.SpawnObject
        }

        private void RebuildMatchObservers(Guid specificMatch)
        {
            foreach (var networkIdentity in matchPlayers[specificMatch])
                if (networkIdentity != null)
                    networkIdentity.RebuildObservers(false);
        }

        #region Observers

        /// <summary>
        /// Callback used by the visibility system to determine if an observer (player) can see this object.
        /// <para>If this function returns true, the network connection will be added as an observer.</para>
        /// </summary>
        /// <param name="player">Network connection of a player.</param>
        /// <returns>True if the player can see this object.</returns>
        public override bool OnCheckObserver(INetworkPlayer player)
        {
            // Not Visible if not in a match
            if (this.MatchId == Guid.Empty)
                return false;

            var networkMatchChecker = player.Identity.GetComponent<NetworkMatchChecker>();

            if (networkMatchChecker == null)
                return false;

            return networkMatchChecker.MatchId == this.MatchId;
        }

        /// <summary>
        /// Callback used by the visibility system to (re)construct the set of observers that can see this object.
        /// <para>Implementations of this callback should add network connections of players that can see this object to the observers set.</para>
        /// </summary>
        /// <param name="observers">The new set of observers for this object.</param>
        /// <param name="initialize">True if the set of observers is being built for the first time.</param>
        public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize)
        {
            if (this.currentMatch == Guid.Empty) return;

            foreach (var networkIdentity in matchPlayers[this.currentMatch])
                if (networkIdentity != null && networkIdentity.Owner != null)
                    observers.Add(networkIdentity.Owner);
        }

        #endregion
    }
}

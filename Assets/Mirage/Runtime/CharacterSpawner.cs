using System;
using System.Collections.Generic;
using Mirage.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Mirage
{

    /// <summary>
    /// Spawns a player as soon as the connection is authenticated
    /// </summary>
    public class CharacterSpawner : MonoBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(CharacterSpawner));

        [FormerlySerializedAs("client")]
        public NetworkClient Client;
        [FormerlySerializedAs("server")]
        public NetworkServer Server;
        [FormerlySerializedAs("sceneManager")]
        public NetworkSceneManager SceneManager;
        [FormerlySerializedAs("clientObjectManager")]
        public ClientObjectManager ClientObjectManager;
        [FormerlySerializedAs("serverObjectManager")]
        public ServerObjectManager ServerObjectManager;
        [FormerlySerializedAs("playerPrefab")]
        public NetworkIdentity PlayerPrefab;

        /// <summary>
        /// Whether to span the player upon connection automatically
        /// </summary>
        public bool AutoSpawn = true;

        // Start is called before the first frame update
        public virtual void Awake()
        {
            if (this.PlayerPrefab == null)
            {
                throw new InvalidOperationException("Assign a player in the CharacterSpawner");
            }
            if (this.Client != null)
            {
                if (this.SceneManager != null)
                {
                    this.SceneManager.OnClientFinishedSceneChange.AddListener(this.OnClientFinishedSceneChange);
                }
                else
                {
                    this.Client.Authenticated.AddListener(this.OnClientAuthenticated);
                    this.Client.Connected.AddListener(this.OnClientConnected);
                }
            }
            if (this.Server != null)
            {
                this.Server.Started.AddListener(this.OnServerStarted);
                if (this.ServerObjectManager == null)
                {
                    throw new InvalidOperationException("Assign a ServerObjectManager");
                }
            }
        }

        private void OnDestroy()
        {
            if (this.Client != null && this.SceneManager != null)
            {
                this.SceneManager.OnClientFinishedSceneChange.RemoveListener(this.OnClientFinishedSceneChange);
                this.Client.Authenticated.RemoveListener(this.OnClientAuthenticated);
            }
            if (this.Server != null)
            {
                this.Server.Started.RemoveListener(this.OnServerStarted);
            }
        }

        internal void OnClientConnected(INetworkPlayer player)
        {
            if (this.ClientObjectManager != null)
            {
                this.ClientObjectManager.RegisterPrefab(this.PlayerPrefab);
            }
            else
            {
                throw new InvalidOperationException("Assign a ClientObjectManager");
            }
        }

        private void OnClientAuthenticated(INetworkPlayer _)
        {
            this.Client.Send(new AddCharacterMessage());
        }

        private void OnServerStarted()
        {
            this.Server.MessageHandler.RegisterHandler<AddCharacterMessage>(this.OnServerAddPlayerInternal);
        }

        /// <summary>
        /// Called on the client when a normal scene change happens.
        /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="sceneOperation">The type of scene load that happened.</param>
        public virtual void OnClientFinishedSceneChange(Scene scene, SceneOperation sceneOperation)
        {
            if (this.AutoSpawn && sceneOperation == SceneOperation.Normal)
                this.RequestServerSpawnPlayer();
        }

        public virtual void RequestServerSpawnPlayer()
        {
            this.Client.Send(new AddCharacterMessage());
        }

        private void OnServerAddPlayerInternal(INetworkPlayer player, AddCharacterMessage msg)
        {
            logger.Log("CharacterSpawner.OnServerAddPlayer");

            if (player.HasCharacter)
            {
                // player already has character on server, but client asked for it
                // so we respawn it here so that client recieves it again
                // this can happen when client loads normally, but server addititively
                this.ServerObjectManager.Spawn(player.Identity);
            }
            else
            {
                this.OnServerAddPlayer(player);
            }
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="player">Connection from client.</param>
        public virtual void OnServerAddPlayer(INetworkPlayer player)
        {
            var startPos = this.GetStartPosition();
            var character = startPos != null
                ? Instantiate(this.PlayerPrefab, startPos.position, startPos.rotation)
                : Instantiate(this.PlayerPrefab);

            this.SetCharacterName(player, character);
            this.ServerObjectManager.AddCharacter(player, character.gameObject);
        }

        protected virtual void SetCharacterName(INetworkPlayer player, NetworkIdentity character)
        {
            // When spawning a player game object, Unity defaults to something like "MyPlayerObject(clone)"
            // which sucks... So let's override it and make it easier to debug. Credit to Mirror for the nice touch.
            character.name = $"{this.PlayerPrefab.name} {player.Address}";
        }

        /// <summary>
        /// This finds a spawn position based on start position objects in the scene.
        /// <para>This is used by the default implementation of OnServerAddPlayer.</para>
        /// </summary>
        /// <returns>Returns the transform to spawn a player at, or null.</returns>
        public virtual Transform GetStartPosition()
        {
            if (this.startPositions.Count == 0)
                return null;

            if (this.playerSpawnMethod == PlayerSpawnMethod.Random)
            {
                return this.startPositions[UnityEngine.Random.Range(0, this.startPositions.Count)];
            }
            else
            {
                var startPosition = this.startPositions[this.startPositionIndex];
                this.startPositionIndex = (this.startPositionIndex + 1) % this.startPositions.Count;
                return startPosition;
            }
        }

        public int startPositionIndex;

        /// <summary>
        /// List of transforms where players can be spawned
        /// </summary>
        public List<Transform> startPositions = new List<Transform>();

        /// <summary>
        /// Enumeration of methods of where to spawn player objects in multiplayer games.
        /// </summary>
        public enum PlayerSpawnMethod { Random, RoundRobin }

        /// <summary>
        /// The current method of spawning players used by the CharacterSpawner.
        /// </summary>
        [Tooltip("Round Robin or Random order of Start Position selection")]
        public PlayerSpawnMethod playerSpawnMethod;
    }
}

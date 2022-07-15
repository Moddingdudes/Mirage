using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirage.Examples.Tanks
{
    public class TankGameManager : MonoBehaviour
    {
        public int MinimumPlayersForGame = 1;

        public Tank LocalPlayer;
        public GameObject StartPanel;
        public GameObject GameOverPanel;
        public GameObject HealthTextLabel;
        public GameObject ScoreTextLabel;
        public Text HealthText;
        public Text ScoreText;
        public Text PlayerNameText;
        public Text WinnerNameText;
        public bool IsGameReady;
        public bool IsGameOver;
        public List<Tank> players = new List<Tank>();
        public NetworkManager NetworkManager;

        private void Update()
        {
            if (this.NetworkManager.IsNetworkActive)
            {
                this.GameReadyCheck();
                this.GameOverCheck();

                if (this.NetworkManager.Client.Active)
                {
                    if (this.LocalPlayer == null)
                    {
                        this.FindLocalTank();
                    }
                    else
                    {
                        this.ShowReadyMenu();
                        this.UpdateStats();
                    }
                }
            }
            else
            {
                //Cleanup state once network goes offline
                this.IsGameReady = false;
                this.LocalPlayer = null;
                this.players.Clear();
            }
        }

        private void ShowReadyMenu()
        {
            if (this.NetworkManager.Client.Active)
            {

                if (this.LocalPlayer.isReady)
                    return;

                this.StartPanel.SetActive(true);
            }
        }

        private void GameReadyCheck()
        {
            if (!this.IsGameReady)
            {
                //Look for connections that are not in the player list
                this.CheckPlayersNotInList();

                //If minimum connections has been check if they are all ready
                if (this.players.Count >= this.MinimumPlayersForGame && this.GetAllReadyState())
                {
                    this.IsGameReady = true;
                    this.AllowTankMovement();

                    //Update Local GUI:
                    this.StartPanel.SetActive(false);
                    this.HealthTextLabel.SetActive(true);
                    this.ScoreTextLabel.SetActive(true);
                }
            }
        }

        private void CheckPlayersNotInList()
        {
            var world = this.NetworkManager.Server.Active ? this.NetworkManager.Server.World : this.NetworkManager.Client.World;
            foreach (var identity in world.SpawnedIdentities)
            {
                var comp = identity.GetComponent<Tank>();
                if (comp != null && !this.players.Contains(comp))
                {
                    //Add if new
                    this.players.Add(comp);
                }
            }
        }

        private bool GetAllReadyState()
        {
            if (!this.LocalPlayer || !this.LocalPlayer.isReady) return false;

            var AllReady = true;
            foreach (var tank in this.players)
            {
                if (!tank.isReady)
                {
                    AllReady = false;
                }
            }
            return AllReady;
        }

        private void GameOverCheck()
        {
            if (!this.IsGameReady)
                return;

            //Cant win a game you play by yourself. But you can still use this example for testing network/movement
            if (this.players.Count == 1)
                return;

            if (this.GetAlivePlayerCount() == 1)
            {
                this.IsGameOver = true;
                this.GameOverPanel.SetActive(true);
                this.DisallowTankMovement();
            }
        }

        private int GetAlivePlayerCount()
        {
            var alivePlayerCount = 0;
            foreach (var tank in this.players)
            {
                if (!tank.IsDead)
                {
                    alivePlayerCount++;

                    //If there is only 1 player left alive this will end up being their name
                    this.WinnerNameText.text = tank.playerName;
                }
            }
            return alivePlayerCount;
        }

        private void FindLocalTank()
        {
            var player = this.NetworkManager.Client.Player;

            // Check to see if the player object is loaded in yet
            if (!player.HasCharacter)
                return;

            this.LocalPlayer = player.Identity.GetComponent<Tank>();
        }

        private void UpdateStats()
        {
            this.HealthText.text = this.LocalPlayer.health.ToString();
            this.ScoreText.text = this.LocalPlayer.score.ToString();
        }

        public void ReadyButtonHandler()
        {
            this.LocalPlayer.SendReadyToServer(this.PlayerNameText.text);
        }

        //All players are ready and game has started. Allow players to move.
        private void AllowTankMovement()
        {
            foreach (var tank in this.players)
            {
                tank.allowMovement = true;
            }
        }

        //Game is over. Prevent movement
        private void DisallowTankMovement()
        {
            foreach (var tank in this.players)
            {
                tank.allowMovement = false;
            }
        }
    }
}

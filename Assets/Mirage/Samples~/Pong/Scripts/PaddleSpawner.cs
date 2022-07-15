using UnityEngine;

namespace Mirage.Examples.Pong
{
    public class PaddleSpawner : CharacterSpawner
    {
        public Transform leftRacketSpawn;
        public Transform rightRacketSpawn;
        public GameObject ballPrefab;
        private GameObject ball;

        public override void Awake()
        {
            base.Awake();

            if (this.Server != null)
            {
                // add disconnect event so that OnServerDisconnect will be called when player disconnects
                this.Server.Disconnected.AddListener(this.OnServerDisconnect);
            }
        }

        // override OnServerAddPlayer so to do custom spawn location for character
        // this method will be called by base class when player sends `AddCharacterMessage`
        public override void OnServerAddPlayer(INetworkPlayer player)
        {
            // add player at correct spawn position
            var start = this.Server.NumberOfPlayers == 0 ? this.leftRacketSpawn : this.rightRacketSpawn;
            var character = Instantiate(this.PlayerPrefab, start.position, start.rotation);
            this.ServerObjectManager.AddCharacter(player, character.gameObject);

            // spawn ball if two players
            if (this.Server.NumberOfPlayers == 2)
            {
                this.ball = Instantiate(this.ballPrefab);
                this.ServerObjectManager.Spawn(this.ball);
            }
        }

        public void OnServerDisconnect(INetworkPlayer _)
        {
            // after 1 player disconnects then destroy the balll
            if (this.ball != null)
                this.ServerObjectManager.Destroy(this.ball);
        }
    }
}

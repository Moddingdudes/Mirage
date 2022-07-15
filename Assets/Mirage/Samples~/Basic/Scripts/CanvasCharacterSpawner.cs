using UnityEngine;

namespace Mirage.Examples.Basic
{
    public class CanvasCharacterSpawner : CharacterSpawner
    {
        [Header("Character Parent")]
        public Transform Parent;

        public override void OnServerAddPlayer(INetworkPlayer player)
        {
            var character = Instantiate(this.PlayerPrefab);
            // Make this a child of the layout panel in the Canvas
            character.transform.SetParent(this.Parent);

            this.SetCharacterName(player, character);
            this.ServerObjectManager.AddCharacter(player, character.gameObject);
        }
    }
}

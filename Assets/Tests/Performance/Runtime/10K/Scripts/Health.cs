namespace Mirage.Examples
{
    public class Health : NetworkBehaviour
    {
        [SyncVar] public int health = 10;

        [Server(error = false)]
        public void Update()
        {
            this.health = (this.health + 1) % 10;
        }
    }
}

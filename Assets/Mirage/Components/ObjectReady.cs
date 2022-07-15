namespace Mirage
{
    public class ObjectReady : NetworkBehaviour
    {
        [SyncVar]
        public bool IsReady;

        [Server]
        public void SetClientReady()
        {
            this.IsReady = true;
        }

        [Server]
        public void SetClientNotReady()
        {
            this.IsReady = false;
        }

        [Client]
        public void Ready()
        {
            this.ReadyRpc();
        }

        [ServerRpc]
        private void ReadyRpc()
        {
            this.IsReady = true;
        }
    }
}

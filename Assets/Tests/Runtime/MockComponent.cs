namespace Mirage.Tests.Runtime
{
    public class MockComponent : NetworkBehaviour
    {
        public int cmdArg1;
        public string cmdArg2;

        [ServerRpc]
        public void Send2Args(int arg1, string arg2)
        {
            this.cmdArg1 = arg1;
            this.cmdArg2 = arg2;
        }


        public INetworkPlayer cmdSender;
        [ServerRpc]
        public void SendWithSender(int arg1, INetworkPlayer sender = null)
        {
            this.cmdArg1 = arg1;
            this.cmdSender = sender;
        }

        public NetworkIdentity cmdNi;

        [ServerRpc]
        public void CmdNetworkIdentity(NetworkIdentity ni)
        {
            this.cmdNi = ni;
        }

        public int rpcArg1;
        public string rpcArg2;

        [ClientRpc]
        public void RpcTest(int arg1, string arg2)
        {
            this.rpcArg1 = arg1;
            this.rpcArg2 = arg2;
        }

        public int targetRpcArg1;
        public string targetRpcArg2;
        public INetworkPlayer targetRpcPlayer;

        [ClientRpc(target = Mirage.RpcTarget.Player)]
        public void ClientConnRpcTest(INetworkPlayer player, int arg1, string arg2)
        {
            this.targetRpcPlayer = player;
            this.targetRpcArg1 = arg1;
            this.targetRpcArg2 = arg2;
        }

        public int rpcOwnerArg1;
        public string rpcOwnerArg2;

        [ClientRpc(target = Mirage.RpcTarget.Owner)]
        public void RpcOwnerTest(int arg1, string arg2)
        {
            this.rpcOwnerArg1 = arg1;
            this.rpcOwnerArg2 = arg2;
        }
    }
}

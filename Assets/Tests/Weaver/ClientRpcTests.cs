using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class ClientRpcTests : TestsBuildFromTestName
    {
        [Test]
        public void ClientRpcValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void ClientRpcOverload()
        {
            this.IsSuccess();
        }

        [Test]
        public void ClientRpcCantBeStatic()
        {
            this.HasError("RpcCantBeStatic must not be static",
                "System.Void ClientRpcTests.ClientRpcCantBeStatic.ClientRpcCantBeStatic::RpcCantBeStatic()");
        }

        [Test]
        public void VirtualClientRpc()
        {
            this.IsSuccess();
        }

        [Test]
        public void OverrideVirtualClientRpc()
        {
            this.IsSuccess();
        }

        [Test]
        public void AbstractClientRpc()
        {
            this.HasError("Abstract Rpcs are currently not supported, use virtual method instead",
                "System.Void ClientRpcTests.AbstractClientRpc.AbstractClientRpc::RpcDoSomething()");
        }

        [Test]
        public void OverrideAbstractClientRpc()
        {
            this.HasError("Abstract Rpcs are currently not supported, use virtual method instead",
                "System.Void ClientRpcTests.OverrideAbstractClientRpc.BaseBehaviour::RpcDoSomething()");
        }

        [Test]
        public void ClientRpcThatExcludesOwner()
        {
            this.IsSuccess();
        }

        [Test]
        public void ClientRpcConnCantSkipNetworkConn()
        {
            this.HasError("ClientRpc with RpcTarget.Player needs a network player parameter", "System.Void ClientRpcTests.ClientRpcConnCantSkipNetworkConn.ClientRpcConnCantSkipNetworkConn::ClientRpcMethod()");
        }

        [Test]
        public void ClientRpcOwnerCantExcludeOwner()
        {
            this.HasError("ClientRpc with RpcTarget.Owner cannot have excludeOwner set as true", "System.Void ClientRpcTests.ClientRpcOwnerCantExcludeOwner.ClientRpcOwnerCantExcludeOwner::ClientRpcMethod()");
        }

        [Test]
        public void CallToRpcBase()
        {
            this.IsSuccess();
        }

        [Test]
        public void CallToNonRpcBase()
        {
            this.IsSuccess();
        }

        [Test]
        public void CallToNonRpcOverLoad()
        {
            this.IsSuccess();
        }

        [Test]
        public void CallToNonRpcOverLoadReverse()
        {
            this.IsSuccess();
        }

        [Test]
        public void RpcAndOverLoad()
        {
            this.IsSuccess();
        }
    }
}

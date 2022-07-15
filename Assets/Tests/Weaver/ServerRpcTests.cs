using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class ServerRpcTests : TestsBuildFromTestName
    {
        [Test]
        public void ServerRpcValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void ServerRpcCantBeStatic()
        {
            this.HasError("CmdCantBeStatic must not be static", "System.Void ServerRpcTests.ServerRpcCantBeStatic.ServerRpcCantBeStatic::CmdCantBeStatic()");
        }

        [Test]
        public void ServerRpcThatIgnoresAuthority()
        {
            this.IsSuccess();
        }

        [Test]
        public void ServerRpcWithArguments()
        {
            this.IsSuccess();
        }

        [Test]
        public void ServerRpcThatIgnoresAuthorityWithSenderConnection()
        {
            this.IsSuccess();
        }

        [Test]
        public void ServerRpcWithSenderConnectionAndOtherArgs()
        {
            this.IsSuccess();
        }

        [Test]
        public void ServerRpcWithSenderConnectionAndOtherArgsWrongOrder()
        {
            this.IsSuccess();
        }

        [Test]
        public void VirtualServerRpc()
        {
            this.IsSuccess();
        }

        [Test]
        public void OverrideVirtualServerRpc()
        {
            this.IsSuccess();
        }

        [Test]
        public void OverrideVirtualCallBaseServerRpc()
        {
            this.IsSuccess();
        }

        [Test]
        public void OverrideVirtualCallsBaseServerRpcWithMultipleBaseClasses()
        {
            this.IsSuccess();
        }

        [Test]
        public void OverrideVirtualCallsBaseServerRpcWithOverride()
        {
            this.IsSuccess();
        }

        [Test]
        public void AbstractServerRpc()
        {
            this.HasError("Abstract Rpcs are currently not supported, use virtual method instead", "System.Void ServerRpcTests.AbstractServerRpc.AbstractServerRpc::CmdDoSomething()");
        }

        [Test]
        public void OverrideAbstractServerRpc()
        {
            this.HasError("Abstract Rpcs are currently not supported, use virtual method instead", "System.Void ServerRpcTests.OverrideAbstractServerRpc.BaseBehaviour::CmdDoSomething()");
        }

        [Test]
        public void ServerRpcWithReturn()
        {
            this.IsSuccess();
        }
    }
}

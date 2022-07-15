using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class NetworkBehaviourTests : TestsBuildFromTestName
    {
        [Test]
        public void NetworkBehaviourValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourAbstractBaseValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourGeneric()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourGenericInherit()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourCmdGenericArgument()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourCmdGenericParam()
        {
            this.HasError("CmdCantHaveGeneric cannot have generic parameters",
                "System.Void NetworkBehaviourTests.NetworkBehaviourCmdGenericParam.NetworkBehaviourCmdGenericParam::CmdCantHaveGeneric()");
        }

        [Test]
        public void NetworkBehaviourCmdCoroutine()
        {
            this.HasError("CmdCantHaveCoroutine cannot be a coroutine",
                "System.Collections.IEnumerator NetworkBehaviourTests.NetworkBehaviourCmdCoroutine.NetworkBehaviourCmdCoroutine::CmdCantHaveCoroutine()");
        }

        [Test]
        public void NetworkBehaviourCmdVoidReturn()
        {
            this.HasError("Use UniTask<System.Int32> to return values from [ServerRpc]",
                "System.Int32 NetworkBehaviourTests.NetworkBehaviourCmdVoidReturn.NetworkBehaviourCmdVoidReturn::CmdCantHaveNonVoidReturn()");
        }

        [Test]
        public void NetworkBehaviourClientRpcGenericArgument()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourClientRpcGenericParam()
        {
            this.HasError("RpcCantHaveGeneric cannot have generic parameters",
                "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcGenericParam.NetworkBehaviourClientRpcGenericParam::RpcCantHaveGeneric()");
        }

        [Test]
        public void NetworkBehaviourClientRpcCoroutine()
        {
            this.HasError("RpcCantHaveCoroutine cannot be a coroutine",
                "System.Collections.IEnumerator NetworkBehaviourTests.NetworkBehaviourClientRpcCoroutine.NetworkBehaviourClientRpcCoroutine::RpcCantHaveCoroutine()");
        }

        [Test]
        public void NetworkBehaviourClientRpcVoidReturn()
        {
            this.HasError("[ClientRpc] must return void",
                "System.Int32 NetworkBehaviourTests.NetworkBehaviourClientRpcVoidReturn.NetworkBehaviourClientRpcVoidReturn::RpcCantHaveNonVoidReturn()");
        }

        [Test]
        public void NetworkBehaviourClientRpcParamOut()
        {
            this.HasError("RpcCantHaveParamOut cannot have out parameters",
                "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcParamOut.NetworkBehaviourClientRpcParamOut::RpcCantHaveParamOut(System.Int32&)");
        }

        [Test]
        public void NetworkBehaviourClientRpcParamOptional()
        {
            this.HasError("RpcCantHaveParamOptional cannot have optional parameters",
                "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcParamOptional.NetworkBehaviourClientRpcParamOptional::RpcCantHaveParamOptional(System.Int32)");
        }

        [Test]
        public void NetworkBehaviourClientRpcParamRef()
        {
            this.HasError("Cannot pass Int32& by reference",
                "System.Int32&");
            this.HasError("Could not process Rpc because one or more of its parameter were invalid",
                "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcParamRef.NetworkBehaviourClientRpcParamRef::RpcCantHaveParamRef(System.Int32&)");
        }

        [Test]
        public void NetworkBehaviourClientRpcParamAbstract()
        {
            this.HasError("Cannot generate write function for abstract class AbstractClass. Use a supported type or provide a custom write function",
                "NetworkBehaviourTests.NetworkBehaviourClientRpcParamAbstract.NetworkBehaviourClientRpcParamAbstract/AbstractClass");
            this.HasError("Could not process Rpc because one or more of its parameter were invalid",
                "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcParamAbstract.NetworkBehaviourClientRpcParamAbstract::RpcCantHaveParamAbstract(NetworkBehaviourTests.NetworkBehaviourClientRpcParamAbstract.NetworkBehaviourClientRpcParamAbstract/AbstractClass)");
        }

        [Test]
        public void NetworkBehaviourClientRpcParamComponent()
        {
            this.HasError("Cannot generate write function for component type ComponentClass. Use a supported type or provide a custom write function",
                "NetworkBehaviourTests.NetworkBehaviourClientRpcParamComponent.NetworkBehaviourClientRpcParamComponent/ComponentClass");
            this.HasError("Could not process Rpc because one or more of its parameter were invalid",
                "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcParamComponent.NetworkBehaviourClientRpcParamComponent::RpcCantHaveParamComponent(NetworkBehaviourTests.NetworkBehaviourClientRpcParamComponent.NetworkBehaviourClientRpcParamComponent/ComponentClass)");
        }

        [Test]
        public void NetworkBehaviourClientRpcParamNetworkConnection()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourClientRpcParamNetworkConnectionNotFirst()
        {
            this.HasError("ClientRpcCantHaveParamOptional has invalid parameter monkeyCon, Cannot pass NetworkConnections", "System.Void NetworkBehaviourTests.NetworkBehaviourClientRpcParamNetworkConnectionNotFirst.NetworkBehaviourClientRpcParamNetworkConnectionNotFirst::ClientRpcCantHaveParamOptional(System.Int32,Mirage.INetworkPlayer)");
        }

        [Test]
        public void NetworkBehaviourClientRpcDuplicateName()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourCmdParamOut()
        {
            this.HasError("CmdCantHaveParamOut cannot have out parameters",
                "System.Void NetworkBehaviourTests.NetworkBehaviourCmdParamOut.NetworkBehaviourCmdParamOut::CmdCantHaveParamOut(System.Int32&)");
        }

        [Test]
        public void NetworkBehaviourCmdParamOptional()
        {
            this.HasError("CmdCantHaveParamOptional cannot have optional parameters",
                "System.Void NetworkBehaviourTests.NetworkBehaviourCmdParamOptional.NetworkBehaviourCmdParamOptional::CmdCantHaveParamOptional(System.Int32)");
        }

        [Test]
        public void NetworkBehaviourCmdParamRef()
        {
            this.HasError("Cannot pass Int32& by reference", "System.Int32&");
            this.HasError("Could not process Rpc because one or more of its parameter were invalid",
                "System.Void NetworkBehaviourTests.NetworkBehaviourCmdParamRef.NetworkBehaviourCmdParamRef::CmdCantHaveParamRef(System.Int32&)");
        }

        [Test]
        public void NetworkBehaviourCmdParamAbstract()
        {
            this.HasError("Cannot generate write function for abstract class AbstractClass. Use a supported type or provide a custom write function",
                "NetworkBehaviourTests.NetworkBehaviourCmdParamAbstract.NetworkBehaviourCmdParamAbstract/AbstractClass");
            this.HasError("Could not process Rpc because one or more of its parameter were invalid",
                "System.Void NetworkBehaviourTests.NetworkBehaviourCmdParamAbstract.NetworkBehaviourCmdParamAbstract::CmdCantHaveParamAbstract(NetworkBehaviourTests.NetworkBehaviourCmdParamAbstract.NetworkBehaviourCmdParamAbstract/AbstractClass)");
        }

        [Test]
        public void NetworkBehaviourCmdParamComponent()
        {
            this.HasError("Cannot generate write function for component type ComponentClass. Use a supported type or provide a custom write function",
                "NetworkBehaviourTests.NetworkBehaviourCmdParamComponent.NetworkBehaviourCmdParamComponent/ComponentClass");
            this.HasError("Could not process Rpc because one or more of its parameter were invalid",
                "System.Void NetworkBehaviourTests.NetworkBehaviourCmdParamComponent.NetworkBehaviourCmdParamComponent::CmdCantHaveParamComponent(NetworkBehaviourTests.NetworkBehaviourCmdParamComponent.NetworkBehaviourCmdParamComponent/ComponentClass)");
        }

        [Test]
        public void NetworkBehaviourCmdParamGameObject()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourCmdDuplicateName()
        {
            this.IsSuccess();
        }

        [Test]
        public void NetworkBehaviourChild()
        {
            this.IsSuccess();
        }
    }
}

using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class BadAttributeUseageTests : TestsBuildFromTestName
    {
        [Test]
        public void MonoBehaviourValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void MonoBehaviourSyncVar()
        {
            this.HasErrorCount(1);
            this.HasError("SyncVar potato must be inside a NetworkBehaviour. MonoBehaviourSyncVar is not a NetworkBehaviour",
              "System.Int32 BadAttributeUseageTests.MonoBehaviourSyncVar.MonoBehaviourSyncVar::potato");
        }

        [Test]
        public void MonoBehaviourSyncList()
        {
            this.HasErrorCount(1);
            this.HasError("potato is a SyncObject and can not be used inside Monobehaviour. MonoBehaviourSyncList is not a NetworkBehaviour",
              "Mirage.Collections.SyncList`1<System.Int32> BadAttributeUseageTests.MonoBehaviourSyncList.MonoBehaviourSyncList::potato");
        }

        [Test]
        public void MonoBehaviourServerRpc()
        {
            this.HasErrorCount(1);
            this.HasError("ServerRpcAttribute method CmdThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.MonoBehaviourServerRpc.MonoBehaviourServerRpc::CmdThisCantBeOutsideNetworkBehaviour()");
        }

        [Test]
        public void MonoBehaviourClientRpc()
        {
            this.HasErrorCount(1);
            this.HasError("ClientRpcAttribute method RpcThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.MonoBehaviourClientRpc.MonoBehaviourClientRpc::RpcThisCantBeOutsideNetworkBehaviour()");
        }

        [Test]
        public void MonoBehaviourServer()
        {
            this.HasErrorCount(1);
            this.HasError("ServerAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.MonoBehaviourServer.MonoBehaviourServer::ThisCantBeOutsideNetworkBehaviour()");
        }

        [Test]
        public void MonoBehaviourServerCallback()
        {
            this.HasErrorCount(1);
            this.HasError("ServerAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.MonoBehaviourServerCallback.MonoBehaviourServerCallback::ThisCantBeOutsideNetworkBehaviour()");
        }

        [Test]
        public void MonoBehaviourClient()
        {
            this.HasErrorCount(1);
            this.HasError("ClientAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.MonoBehaviourClient.MonoBehaviourClient::ThisCantBeOutsideNetworkBehaviour()");
        }

        [Test]
        public void MonoBehaviourClientCallback()
        {
            this.HasErrorCount(1);
            this.HasError("ClientAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.MonoBehaviourClientCallback.MonoBehaviourClientCallback::ThisCantBeOutsideNetworkBehaviour()");
        }


        [Test]
        public void NormalClassClient()
        {
            this.HasErrorCount(1);
            this.HasError("ClientAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.NormalClassClient.NormalClassClient::ThisCantBeOutsideNetworkBehaviour()");
        }
        [Test]
        public void NormalClassClientCallback()
        {
            this.HasErrorCount(1);
            this.HasError("ClientAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.NormalClassClientCallback.NormalClassClientCallback::ThisCantBeOutsideNetworkBehaviour()");
        }
        [Test]
        public void NormalClassClientRpc()
        {
            this.HasErrorCount(1);
            this.HasError("ClientRpcAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.NormalClassClientRpc.NormalClassClientRpc::ThisCantBeOutsideNetworkBehaviour()");
        }
        [Test]
        public void NormalClassServer()
        {
            this.HasErrorCount(1);
            this.HasError("ServerAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.NormalClassServer.NormalClassServer::ThisCantBeOutsideNetworkBehaviour()");
        }
        [Test]
        public void NormalClassServerCallback()
        {
            this.HasErrorCount(1);
            this.HasError("ServerAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.NormalClassServerCallback.NormalClassServerCallback::ThisCantBeOutsideNetworkBehaviour()");
        }
        [Test]
        public void NormalClassServerRpc()
        {
            this.HasErrorCount(1);
            this.HasError("ServerRpcAttribute method ThisCantBeOutsideNetworkBehaviour must be declared in a NetworkBehaviour",
              "System.Void BadAttributeUseageTests.NormalClassServerRpc.NormalClassServerRpc::ThisCantBeOutsideNetworkBehaviour()");
        }
        [Test]
        public void NormalClassSyncVar()
        {
            this.HasErrorCount(1);
            this.HasError("SyncVar potato must be inside a NetworkBehaviour. NormalClassSyncVar is not a NetworkBehaviour",
              "System.Int32 BadAttributeUseageTests.NormalClassSyncVar.NormalClassSyncVar::potato");
        }
    }
}

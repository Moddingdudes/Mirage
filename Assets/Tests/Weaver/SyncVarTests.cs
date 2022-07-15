using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class SyncVarTests : TestsBuildFromTestName
    {
        [Test]
        public void SyncVarsValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarsValidInitialOnly()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarArraySegment()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarsDerivedNetworkBehaviour()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarsStatic()
        {
            this.HasError("invalidVar cannot be static",
                "System.Int32 SyncVarTests.SyncVarsStatic.SyncVarsStatic::invalidVar");
        }

        [Test]
        public void SyncVarsGenericField()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarsGenericParam()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarsInterface()
        {
            this.HasError("Cannot generate write function for interface IMySyncVar. Use a supported type or provide a custom write function",
                "SyncVarTests.SyncVarsInterface.SyncVarsInterface/IMySyncVar SyncVarTests.SyncVarsInterface.SyncVarsInterface::invalidVar");
        }

        [Test]
        public void SyncVarsUnityComponent()
        {
            this.HasError("Cannot generate write function for component type TextMesh. Use a supported type or provide a custom write function",
                "UnityEngine.TextMesh SyncVarTests.SyncVarsUnityComponent.SyncVarsUnityComponent::invalidVar");
        }

        [Test]
        public void SyncVarsCantBeArray()
        {
            this.HasError("thisShouldntWork has invalid type. Use SyncLists instead of arrays",
                "System.Int32[] SyncVarTests.SyncVarsCantBeArray.SyncVarsCantBeArray::thisShouldntWork");
        }

        [Test]
        public void SyncVarsSyncList()
        {
            this.HasError("syncints has [SyncVar] attribute. ISyncObject should not be marked with SyncVar",
                "Mirage.Collections.SyncList`1<System.Int32> SyncVarTests.SyncVarsSyncList.SyncVarsSyncList::syncints");
        }

        [Test]
        public void SyncVarsMoreThan63()
        {
            this.HasError("SyncVarsMoreThan63 has too many [SyncVar]. Consider refactoring your class into multiple components",
                "SyncVarTests.SyncVarsMoreThan63.SyncVarsMoreThan63");
        }
    }
}

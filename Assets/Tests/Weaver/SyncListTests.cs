using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class SyncListTests : TestsBuildFromTestName
    {
        [Test]
        public void SyncList()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListByteValid()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListGenericAbstractInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListGenericInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListGenericInheritanceWithMultipleGeneric()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListInheritance()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListNestedStruct()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListNestedInAbstractClass()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListNestedInAbstractClassWithInvalid()
        {
            this.HasError("Cannot generate write function for Object. Use a supported type or provide a custom write function",
                "UnityEngine.Object");

            this.HasError("Cannot generate read function for Object. Use a supported type or provide a custom read function",
                "UnityEngine.Object");
        }

        [Test]
        public void SyncListNestedInStruct()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListNestedInStructWithInvalid()
        {
            this.HasError("Cannot generate write function for Object. Use a supported type or provide a custom write function",
                "UnityEngine.Object");

            this.HasError("Cannot generate read function for Object. Use a supported type or provide a custom read function",
                "UnityEngine.Object");
        }

        [Test]
        public void SyncListStruct()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListErrorForGenericStruct()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncListErrorForInterface()
        {
            this.HasError("Cannot generate read function for interface MyInterface. Use a supported type or provide a custom read function",
                "SyncListTests.SyncListErrorForInterface.MyInterface");
            this.HasError("Cannot generate write function for interface MyInterface. Use a supported type or provide a custom write function",
                "SyncListTests.SyncListErrorForInterface.MyInterface");
        }

        [Test]
        public void SyncListErrorWhenUsingGenericListInNetworkBehaviour()
        {
            this.IsSuccess();
        }
    }
}

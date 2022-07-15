using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class GeneratedReaderWriterTests : TestsBuildFromTestName
    {
        [SetUp]
        public override void TestSetup()
        {
            base.TestSetup();
        }

        [Test]
        public void CreatesForStructs()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreateForExplicitNetworkMessage()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForClass()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForClassInherited()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForClassWithValidConstructor()
        {
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorForClassWithNoValidConstructor()
        {
            this.HasError("SomeOtherData can't be deserialized because it has no default constructor",
                "GeneratedReaderWriter.GivesErrorForClassWithNoValidConstructor.SomeOtherData");
        }

        [Test]
        public void CreatesForInheritedFromScriptableObject()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForStructFromDifferentAssemblies()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForClassFromDifferentAssemblies()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForClassFromDifferentAssembliesWithValidConstructor()
        {
            this.IsSuccess();
        }

        [Test]
        public void CanUseCustomReadWriteForTypesFromDifferentAssemblies()
        {
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorWhenUsingUnityAsset()
        {
            this.HasError("Material can't be deserialized because it has no default constructor",
                "UnityEngine.Material");
        }

        [Test]
        public void GivesErrorWhenUsingObject()
        {
            this.HasError("Cannot generate write function for Object. Use a supported type or provide a custom write function",
                "UnityEngine.Object");
        }

        [Test]
        public void GivesErrorWhenUsingScriptableObject()
        {
            this.HasError("Cannot generate write function for ScriptableObject. Use a supported type or provide a custom write function",
                "UnityEngine.ScriptableObject");
        }

        [Test]
        public void GivesErrorWhenUsingMonoBehaviour()
        {
            this.HasError("Cannot generate write function for component type MonoBehaviour. Use a supported type or provide a custom write function",
                "UnityEngine.MonoBehaviour");
        }

        [Test]
        public void GivesErrorWhenUsingTypeInheritedFromMonoBehaviour()
        {
            this.HasError("Cannot generate write function for component type MyBehaviour. Use a supported type or provide a custom write function",
                "GeneratedReaderWriter.GivesErrorWhenUsingTypeInheritedFromMonoBehaviour.MyBehaviour");
        }

        [Test]
        public void ExcludesNonSerializedFields()
        {
            // we test this by having a not allowed type in the class, but mark it with NonSerialized
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorWhenUsingInterface()
        {
            this.HasError("Cannot generate write function for interface IData. Use a supported type or provide a custom write function",
                "GeneratedReaderWriter.GivesErrorWhenUsingInterface.IData");
        }

        [Test]
        public void CanUseCustomReadWriteForInterfaces()
        {
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorWhenUsingAbstractClass()
        {
            this.HasError("Cannot generate write function for abstract class DataBase. Use a supported type or provide a custom write function", "GeneratedReaderWriter.GivesErrorWhenUsingAbstractClass.DataBase");
        }

        [Test]
        public void CanUseCustomReadWriteForAbstractClass()
        {
            this.IsSuccess();
        }

        [Test]
        public void CanUseCustomReadWriteForAbstractClassUsedInMessage()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForEnums()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForArraySegment()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForStructArraySegment()
        {
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorForJaggedArray()
        {
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorForMultidimensionalArray()
        {
            this.HasError("Int32[0...,0...] is an unsupported type. Multidimensional arrays are not supported",
                "System.Int32[0...,0...]");
        }

        [Test]
        public void GivesErrorForInvalidArrayType()
        {
            this.HasError("Cannot generate write function for component type MonoBehaviour. Use a supported type or provide a custom write function", "UnityEngine.MonoBehaviour");
        }

        [Test]
        public void GivesErrorForInvalidArraySegmentType()
        {
            this.HasError("Cannot generate write function for component type MonoBehaviour. Use a supported type or provide a custom write function", "UnityEngine.MonoBehaviour");
        }

        [Test]
        public void CreatesForList()
        {
            this.IsSuccess();
        }

        [Test]
        public void CreatesForStructList()
        {
            this.IsSuccess();
        }

        [Test]
        public void GivesErrorForInvalidListType()
        {
            this.HasError("Cannot generate write function for component type MonoBehaviour. Use a supported type or provide a custom write function", "UnityEngine.MonoBehaviour");
        }

        [Test]
        public void CreatesForNullable()
        {
            this.IsSuccess();
        }
    }
}

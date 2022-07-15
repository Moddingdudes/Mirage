using NUnit.Framework;

namespace Mirage.Tests.Weaver
{
    public class SyncVarHookTests : TestsBuildFromTestName
    {
        private string TypeName()
        {
            var name = TestContext.CurrentContext.Test.Name;
            var ClassName = nameof(SyncVarHookTests);
            // standard format for test name
            return $"{ClassName}.{name}.{name}";
        }


        [Test]
        public void FindsPrivateHook()
        {
            this.IsSuccess();
        }

        [Test]
        public void FindsPublicHook()
        {
            this.IsSuccess();
        }

        [Test]
        public void FindsStaticHook()
        {
            this.IsSuccess();
        }

        [Test]
        public void FindsHookWithNetworkIdentity()
        {
            this.IsSuccess();
        }

        [Test]
        public void FindsHookWithGameObject()
        {
            this.IsSuccess();
        }

        [Test]
        public void FindsHookWithOtherOverloadsInOrder()
        {
            this.IsSuccess();
        }

        [Test]
        public void FindsHookWithOtherOverloadsInReverseOrder()
        {
            this.IsSuccess();
        }

        [Test]
        public void ErrorWhenNoHookFound()
        {
            this.HasError($"Could not find hook for 'health', hook name 'onChangeHealth', hook type Automatic. See SyncHookType for valid signatures",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ErrorWhenNoHookWithCorrectParametersFound()
        {
            this.HasError($"Could not find hook for 'health', hook name 'onChangeHealth', hook type Automatic. See SyncHookType for valid signatures",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ErrorForWrongTypeOldParameter()
        {
            this.HasError($"Wrong type for Parameter in hook for 'health', hook name 'onChangeHealth'.",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ErrorForWrongTypeNewParameter()
        {
            this.HasError($"Wrong type for Parameter in hook for 'health', hook name 'onChangeHealth'.",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void FindsHookEvent()
        {
            this.IsSuccess();
        }

        [Test]
        public void ErrorWhenHookNotAction()
        {
            this.HasError($"Hook Event for 'health' needs to be type 'System.Action<,>' but was 'SyncVarHookTests.ErrorWhenHookNotAction.DoStuff' instead",
                $"SyncVarHookTests.ErrorWhenHookNotAction.DoStuff {this.TypeName()}::OnChangeHealth");
        }

        [Test]
        public void ErrorWhenNotGenericAction()
        {
            this.HasError($"Hook Event for 'health' needs to be type 'System.Action<,>' but was 'System.Action' instead",
                $"System.Action {this.TypeName()}::OnChangeHealth");
        }

        [Test]
        public void ErrorWhenEventArgsAreWrong()
        {
            this.HasError($"Hook Event for 'health' needs to be type 'System.Action<,>' but was 'System.Action`2<System.Int32,System.Single>' instead",
                $"System.Action`2<System.Int32,System.Single> {this.TypeName()}::OnChangeHealth");
        }

        [Test]
        public void SyncVarHookServer()
        {
            this.IsSuccess();
        }

        [Test]
        public void SyncVarHookServerError()
        {
            this.HasError($"'invokeHookOnServer' is set to true but no hook was implemented. Please implement hook or set 'invokeHookOnServer' back to false or remove for default false.",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void AutomaticHookMethod1()
        {
            this.IsSuccess();
        }

        [Test]
        public void AutomaticHookMethod2()
        {
            this.IsSuccess();
        }

        [Test]
        public void AutomaticHookEvent1()
        {
            this.IsSuccess();
        }

        [Test]
        public void AutomaticHookEvent2()
        {
            this.IsSuccess();
        }

        [Test]
        public void AutomaticNotFound()
        {
            this.HasError("Could not find hook for 'health', hook name 'onChangeHealth', hook type Automatic. See SyncHookType for valid signatures",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void AutomaticFound2Methods()
        {
            this.HasError("Mutliple hooks found for 'health', hook name 'onChangeHealth'. Please set HookType or remove one of the overloads",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ExplicitEvent1Found()
        {
            this.IsSuccess();
        }

        [Test]
        public void ExplicitEvent2Found()
        {
            this.IsSuccess();
        }

        [Test]
        public void ExplicitMethod1Found()
        {
            this.IsSuccess();
        }

        [Test]
        public void ExplicitMethod2Found()
        {
            this.IsSuccess();
        }

        [Test]
        public void ExplicitMethod1FoundWithOverLoad()
        {
            this.IsSuccess();
        }

        [Test]
        public void ExplicitMethod2FoundWithOverLoad()
        {
            this.IsSuccess();
        }

        [Test]
        public void ExplicitMethod1NotFound()
        {
            this.HasError("Could not find hook for 'health', hook name 'onChangeHealth', hook type MethodWith1Arg. See SyncHookType for valid signatures",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ExplicitMethod2NotFound()
        {
            this.HasError("Could not find hook for 'health', hook name 'onChangeHealth', hook type MethodWith2Arg. See SyncHookType for valid signatures",
                $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ExplicitEvent1NotFound()
        {
            this.HasError("Could not find hook for 'health', hook name 'onChangeHealth', hook type EventWith1Arg. See SyncHookType for valid signatures",
                 $"System.Int32 {this.TypeName()}::health");
        }

        [Test]
        public void ExplicitEvent2NotFound()
        {
            this.HasError("Could not find hook for 'health', hook name 'onChangeHealth', hook type EventWith2Arg. See SyncHookType for valid signatures",
                 $"System.Int32 {this.TypeName()}::health");
        }
    }
}

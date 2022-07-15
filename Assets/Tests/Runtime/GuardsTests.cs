using Mirage.Tests.Runtime.ClientServer;
using NUnit.Framework;
using UnityEngine;

namespace Mirage.Tests.Runtime
{
    public class ExampleGuards : NetworkBehaviour
    {
        public bool serverFunctionCalled;
        public bool serverCallbackFunctionCalled;
        public bool clientFunctionCalled;
        public bool clientCallbackFunctionCalled;
        public bool hasAuthorityCalled;
        public bool hasAuthorityNoErrorCalled;
        public bool localPlayerCalled;
        public bool localPlayerNoErrorCalled;

        [Server]
        public void CallServerFunction()
        {
            this.serverFunctionCalled = true;
        }

        [Server(error = false)]
        public void CallServerCallbackFunction()
        {
            this.serverCallbackFunctionCalled = true;
        }

        [Client]
        public void CallClientFunction()
        {
            this.clientFunctionCalled = true;
        }

        [Client(error = false)]
        public void CallClientCallbackFunction()
        {
            this.clientCallbackFunctionCalled = true;
        }

        [HasAuthority]
        public void CallAuthorityFunction()
        {
            this.hasAuthorityCalled = true;
        }

        [HasAuthority(error = false)]
        public void CallAuthorityNoErrorFunction()
        {
            this.hasAuthorityNoErrorCalled = true;
        }

        [LocalPlayer]
        public void CallLocalPlayer()
        {
            this.localPlayerCalled = true;
        }

        [LocalPlayer(error = false)]
        public void CallLocalPlayerNoError()
        {
            this.localPlayerNoErrorCalled = true;
        }
    }

    public class GuardsTests : ClientServerSetup<ExampleGuards>
    {

        [Test]
        public void CanCallServerFunctionAsServer()
        {
            this.serverComponent.CallServerFunction();
            Assert.That(this.serverComponent.serverFunctionCalled, Is.True);
        }

        [Test]
        public void CanCallServerFunctionCallbackAsServer()
        {
            this.serverComponent.CallServerCallbackFunction();
            Assert.That(this.serverComponent.serverCallbackFunctionCalled, Is.True);
        }

        [Test]
        public void CannotCallClientFunctionAsServer()
        {
            Assert.Throws<MethodInvocationException>(() =>
            {
                this.serverComponent.CallClientFunction();
            });
        }

        [Test]
        public void CannotCallClientCallbackFunctionAsServer()
        {
            this.serverComponent.CallClientCallbackFunction();
            Assert.That(this.serverComponent.clientCallbackFunctionCalled, Is.False);
        }

        [Test]
        public void CannotCallServerFunctionAsClient()
        {
            Assert.Throws<MethodInvocationException>(() =>
            {
                this.clientComponent.CallServerFunction();
            });
        }

        [Test]
        public void CannotCallServerFunctionCallbackAsClient()
        {
            this.clientComponent.CallServerCallbackFunction();
            Assert.That(this.clientComponent.serverCallbackFunctionCalled, Is.False);
        }

        [Test]
        public void CanCallClientFunctionAsClient()
        {
            this.clientComponent.CallClientFunction();
            Assert.That(this.clientComponent.clientFunctionCalled, Is.True);
        }

        [Test]
        public void CanCallClientCallbackFunctionAsClient()
        {
            this.clientComponent.CallClientCallbackFunction();
            Assert.That(this.clientComponent.clientCallbackFunctionCalled, Is.True);
        }

        [Test]
        public void CanCallHasAuthorityFunctionAsClient()
        {
            this.clientComponent.CallAuthorityFunction();
            Assert.That(this.clientComponent.hasAuthorityCalled, Is.True);
        }

        [Test]
        public void CanCallHasAuthorityCallbackFunctionAsClient()
        {
            this.clientComponent.CallAuthorityNoErrorFunction();
            Assert.That(this.clientComponent.hasAuthorityNoErrorCalled, Is.True);
        }

        [Test]
        public void GuardHasAuthorityError()
        {
            var obj = new GameObject("randomObject", typeof(NetworkIdentity), typeof(ExampleGuards));
            var guardedComponent = obj.GetComponent<ExampleGuards>();
            Assert.Throws<MethodInvocationException>(() =>
           {
               guardedComponent.CallAuthorityFunction();
           });

            Object.Destroy(obj);
        }

        [Test]
        public void GuardHasAuthorityNoError()
        {
            var obj = new GameObject("randomObject", typeof(NetworkIdentity), typeof(ExampleGuards));
            var guardedComponent = obj.GetComponent<ExampleGuards>();
            guardedComponent.CallAuthorityNoErrorFunction();
            Assert.That(guardedComponent.hasAuthorityNoErrorCalled, Is.False);

            Object.Destroy(obj);
        }

        [Test]
        public void CanCallLocalPlayer()
        {
            this.clientComponent.CallLocalPlayer();
            Assert.That(this.clientComponent.localPlayerCalled, Is.True);
        }

        [Test]
        public void CanCallLocalPlayerNoError()
        {
            this.clientComponent.CallLocalPlayerNoError();
            Assert.That(this.clientComponent.localPlayerNoErrorCalled, Is.True);
        }

        [Test]
        public void GuardLocalPlayer()
        {
            var obj = new GameObject("randomObject", typeof(NetworkIdentity), typeof(ExampleGuards));
            var guardedComponent = obj.GetComponent<ExampleGuards>();

            Assert.Throws<MethodInvocationException>(() =>
            {
                guardedComponent.CallLocalPlayer();

            });

            Object.Destroy(obj);
        }

        [Test]
        public void GuardLocalPlayerNoError()
        {
            var obj = new GameObject("randomObject", typeof(NetworkIdentity), typeof(ExampleGuards));
            var guardedComponent = obj.GetComponent<ExampleGuards>();

            guardedComponent.CallLocalPlayerNoError();
            Assert.That(guardedComponent.localPlayerNoErrorCalled, Is.False);

            Object.Destroy(obj);
        }
    }
}

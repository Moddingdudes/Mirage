using System;
using Mirage.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Mirage.Tests.Runtime
{
    internal abstract class SyncVarHookTesterBase : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnValue1Changed))]
        public float value1;
        [SyncVar(hook = nameof(OnValue2Changed))]
        public float value2;

        public event Action OnValue2ChangedVirtualCalled;

        public abstract void OnValue1Changed(float old, float newValue);
        public virtual void OnValue2Changed(float old, float newValue)
        {
            OnValue2ChangedVirtualCalled?.Invoke();
        }

        public void ChangeValues()
        {
            this.value1 += 1f;
            this.value2 += 1f;
        }

        public void CallOnValue2Changed()
        {
            this.OnValue2Changed(1, 1);
        }
    }

    internal class SyncVarHookTester : SyncVarHookTesterBase
    {
        public event Action OnValue1ChangedOverrideCalled;
        public event Action OnValue2ChangedOverrideCalled;
        public override void OnValue1Changed(float old, float newValue)
        {
            OnValue1ChangedOverrideCalled?.Invoke();
        }
        public override void OnValue2Changed(float old, float newValue)
        {
            OnValue2ChangedOverrideCalled?.Invoke();
        }
    }
    [TestFixture]
    public class SyncVarVirtualTest
    {
        private SyncVarHookTester serverTester;
        private NetworkIdentity netIdServer;
        private SyncVarHookTester clientTester;
        private NetworkIdentity netIdClient;
        private readonly NetworkWriter ownerWriter = new NetworkWriter(1300);
        private readonly NetworkWriter observersWriter = new NetworkWriter(1300);
        private readonly NetworkReader reader = new NetworkReader();

        [SetUp]
        public void Setup()
        {
            // create server and client objects and sync inital values

            var gameObject1 = new GameObject();
            this.netIdServer = gameObject1.AddComponent<NetworkIdentity>();
            this.serverTester = gameObject1.AddComponent<SyncVarHookTester>();

            var gameObject2 = new GameObject();
            this.netIdClient = gameObject2.AddComponent<NetworkIdentity>();
            this.clientTester = gameObject2.AddComponent<SyncVarHookTester>();

            this.serverTester.value1 = 1;
            this.serverTester.value2 = 2;

            this.SyncValuesWithClient();
        }

        private void SyncValuesWithClient()
        {
            this.ownerWriter.Reset();
            this.observersWriter.Reset();

            this.netIdServer.OnSerializeAll(true, this.ownerWriter, this.observersWriter);

            // apply all the data from the server object

            this.reader.Reset(this.ownerWriter.ToArraySegment());
            this.netIdClient.OnDeserializeAll(this.reader, true);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(this.serverTester.gameObject);
            UnityEngine.Object.DestroyImmediate(this.clientTester.gameObject);

            this.ownerWriter.Reset();
            this.observersWriter.Reset();
            this.reader.Dispose();
        }
        [Test]
        public void AbstractMethodOnChangeWorkWithHooks()
        {
            this.serverTester.ChangeValues();

            var value1OverrideCalled = false;
            this.clientTester.OnValue1ChangedOverrideCalled += () =>
            {
                value1OverrideCalled = true;
            };

            this.SyncValuesWithClient();

            Assert.AreEqual(this.serverTester.value1, this.serverTester.value1);
            Assert.IsTrue(value1OverrideCalled);
        }
        [Test]
        public void VirtualMethodOnChangeWorkWithHooks()
        {
            this.serverTester.ChangeValues();

            var value2OverrideCalled = false;
            this.clientTester.OnValue2ChangedOverrideCalled += () =>
            {
                value2OverrideCalled = true;
            };

            var value2VirtualCalled = false;
            this.clientTester.OnValue2ChangedVirtualCalled += () =>
            {
                value2VirtualCalled = true;
            };

            this.SyncValuesWithClient();

            Assert.AreEqual(this.serverTester.value2, this.serverTester.value2);
            Assert.IsTrue(value2OverrideCalled, "Override method not called");
            Assert.IsFalse(value2VirtualCalled, "Virtual method called when Override exists");
        }

        [Test]
        public void ManuallyCallingVirtualMethodCallsOverride()
        {
            // this to check that class are set up correct for tests above
            this.serverTester.ChangeValues();

            var value2OverrideCalled = false;
            this.clientTester.OnValue2ChangedOverrideCalled += () =>
            {
                value2OverrideCalled = true;
            };

            var value2VirtualCalled = false;
            this.clientTester.OnValue2ChangedVirtualCalled += () =>
            {
                value2VirtualCalled = true;
            };

            var baseClass = this.clientTester as SyncVarHookTesterBase;
            baseClass.OnValue2Changed(1, 1);

            Assert.AreEqual(this.serverTester.value2, this.serverTester.value2);
            Assert.IsTrue(value2OverrideCalled, "Override method not called");
            Assert.IsFalse(value2VirtualCalled, "Virtual method called when Override exists");
        }
        [Test]
        public void ManuallyCallingVirtualMethodInsideBaseClassCallsOverride()
        {
            // this to check that class are set up correct for tests above
            this.serverTester.ChangeValues();

            var value2OverrideCalled = false;
            this.clientTester.OnValue2ChangedOverrideCalled += () =>
            {
                value2OverrideCalled = true;
            };

            var value2VirtualCalled = false;
            this.clientTester.OnValue2ChangedVirtualCalled += () =>
            {
                value2VirtualCalled = true;
            };

            var baseClass = this.clientTester as SyncVarHookTesterBase;
            baseClass.CallOnValue2Changed();

            Assert.AreEqual(this.serverTester.value2, this.serverTester.value2);
            Assert.IsTrue(value2OverrideCalled, "Override method not called");
            Assert.IsFalse(value2VirtualCalled, "Virtual method called when Override exists");
        }
    }
}

using System;
using System.Collections;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer.Generics
{
    public class WithGenericSyncVarBig_behaviour<T> : NetworkBehaviour
    {
        public event Action<T, T> hookMethod;

        [SyncVar(hook = nameof(onValueChanged))]
        public T value1;

        private void onValueChanged(T oldValue, T newValue)
        {
            hookMethod?.Invoke(oldValue, newValue);
        }

        public event Action<T, T> hookEvent;

        [SyncVar(hook = nameof(hookEvent))]
        public T value2;
        [SyncVar]
        public int value3;
        [SyncVar]
        public T value4;

        public T valueNotVar;
    }

    public class WithGenericSyncVarBig_behaviourInt : WithGenericSyncVarBig_behaviour<int>
    {
    }
    public class WithGenericSyncVarBig_behaviourObject : WithGenericSyncVarBig_behaviour<MyClass>
    {
    }

    public class WithGenericSyncVarBigInt : ClientServerSetup<WithGenericSyncVarBig_behaviourInt>
    {
        [Test]
        public void DoesNotError()
        {
            // passes setup without errors
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator HookEventCalled()
        {
            const int num1 = 32;
            const int num2 = 132;
            const int num3 = 232;
            const int num4 = 332;
            var hook1 = Substitute.For<Action<int, int>>();
            var hook2 = Substitute.For<Action<int, int>>();
            this.clientComponent.hookMethod += hook1;
            this.clientComponent.hookEvent += hook2;

            this.serverComponent.value1 = num1;
            this.serverComponent.value2 = num2;
            this.serverComponent.value3 = num3;
            this.serverComponent.value4 = num4;


            var writer = new NetworkWriter(500);
            this.serverComponent.SerializeSyncVars(writer, false);
            var reader = new NetworkReader();
            reader.Reset(writer.ToArraySegment());

            this.clientComponent.DeserializeSyncVars(reader, false);

            hook1.Received(1).Invoke(0, num1);
            //hook2.Received(1).Invoke(0, num2);
            Assert.That(this.clientComponent.value1, Is.EqualTo(num1));
            Assert.That(this.clientComponent.value2, Is.EqualTo(num2));
            Assert.That(this.clientComponent.value3, Is.EqualTo(num3));
            Assert.That(this.clientComponent.value4, Is.EqualTo(num4));

            yield return null;
            yield return null;
        }
    }
    public class WithGenericSyncVarBigObject : ClientServerSetup<WithGenericSyncVarBig_behaviourObject>
    {
        [Test]
        public void DoesNotError()
        {
            // passes setup without errors
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator HookEventCalled()
        {
            const int num1 = 32;
            const int num2 = 132;
            const int num3 = 232;
            const int num4 = 332;
            var hook1 = Substitute.For<Action<MyClass, MyClass>>();
            var hook2 = Substitute.For<Action<MyClass, MyClass>>();
            this.clientComponent.hookMethod += hook1;
            this.clientComponent.hookEvent += hook2;

            this.serverComponent.value1 = new MyClass { Value = num1 };
            this.serverComponent.value2 = new MyClass { Value = num2 };
            this.serverComponent.value3 = num3;
            this.serverComponent.value4 = new MyClass { Value = num4 };

            yield return null;
            yield return null;

            hook1.Received(1).Invoke(
                Arg.Is<MyClass>(x => x == null),
                Arg.Is<MyClass>(x => x != null && x.Value == num1)
            );
            hook2.Received(1).Invoke(
                Arg.Is<MyClass>(x => x == null),
                Arg.Is<MyClass>(x => x != null && x.Value == num2)
            );
            Assert.That(this.clientComponent.value1, Is.Not.Null);
            Assert.That(this.clientComponent.value2, Is.Not.Null);
            Assert.That(this.clientComponent.value3, Is.Not.Null);
            Assert.That(this.clientComponent.value4, Is.Not.Null);
            Assert.That(this.clientComponent.value1.Value, Is.EqualTo(num1));
            Assert.That(this.clientComponent.value2.Value, Is.EqualTo(num2));
            Assert.That(this.clientComponent.value3, Is.EqualTo(num3));
            Assert.That(this.clientComponent.value4.Value, Is.EqualTo(num4));
        }
    }
}

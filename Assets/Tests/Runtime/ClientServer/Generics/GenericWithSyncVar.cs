using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer.Generics
{
    // note, this can't be a behaviour by itself, it must have a child class in order to be used
    public class GenericWithSyncVarBase_behaviour<T> : NetworkBehaviour
    {
        [SyncVar] public int baseValue;

    }
    public class GenericWithSyncVar_behaviour<T> : GenericWithSyncVarBase_behaviour<T>
    {
        [SyncVar] public int value;
    }

    public class GenericWithSyncVar_behaviourInt : GenericWithSyncVar_behaviour<int>
    {
        [SyncVar] public int moreValue;
    }
    public class GenericWithSyncVar_behaviourObject : GenericWithSyncVar_behaviour<object>
    {
        [SyncVar] public int moreValue;
    }

    public class GenericWithSyncVarInt : ClientServerSetup<GenericWithSyncVar_behaviourInt>
    {
        [Test]
        public void DoesNotError()
        {
            // passes setup without errors
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator SyncToClient()
        {
            const int num1 = 11;
            const int num2 = 12;
            const int num3 = 13;
            this.serverComponent.baseValue = num1;
            this.serverComponent.value = num2;
            this.serverComponent.moreValue = num3;

            yield return null;
            yield return null;

            Assert.That(this.clientComponent.baseValue, Is.EqualTo(num1));
            Assert.That(this.clientComponent.value, Is.EqualTo(num2));
            Assert.That(this.clientComponent.moreValue, Is.EqualTo(num3));
        }
    }
    public class GenericWithSyncVarObject : ClientServerSetup<GenericWithSyncVar_behaviourObject>
    {
        [Test]
        public void DoesNotError()
        {
            // passes setup without errors
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator SyncToClient()
        {
            const int num1 = 11;
            const int num2 = 12;
            const int num3 = 13;
            this.serverComponent.baseValue = num1;
            this.serverComponent.value = num2;
            this.serverComponent.moreValue = num3;

            yield return null;
            yield return null;

            Assert.That(this.clientComponent.baseValue, Is.EqualTo(num1));
            Assert.That(this.clientComponent.value, Is.EqualTo(num2));
            Assert.That(this.clientComponent.moreValue, Is.EqualTo(num3));
        }
    }
}

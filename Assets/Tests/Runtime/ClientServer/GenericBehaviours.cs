using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// extreme example/edge case to make sure generic really work
// These check the Weaver methods: GetMethodInBaseType and MatchGenericParameters
namespace Mirage.Tests.Runtime.ClientServer.GenericBehaviours.NoMiddle
{
    public class With3<A, B, C> : NetworkBehaviour
    {
        [SyncVar] public int a;
    }

    public class With2<D, E> : With3<int, E, D>
    {
    }

    public class With0 : With2<Vector3, float>
    {
        [SyncVar] public float b;
    }

    public class GenericNetworkBehaviorSyncvarNoMiddleTest : ClientServerSetup<With0>
    {
        [UnityTest]
        public IEnumerator CanSyncValues()
        {
            this.serverComponent.a = 10;
            this.serverComponent.b = 12.5f;
            yield return new WaitForSeconds(0.2f);

            Assert.That(this.clientComponent.a, Is.EqualTo(10));
            Assert.That(this.clientComponent.b, Is.EqualTo(12.5f));
        }
    }
}
namespace Mirage.Tests.Runtime.ClientServer.GenericBehaviours.SyncVarMiddle
{
    public class With3<A, B, C> : NetworkBehaviour
    {
        [SyncVar] public int a;
    }

    public class With2<D, E> : With3<int, E, D>
    {
        [SyncVar] public int b;
    }

    public class With0 : With2<Vector3, float>
    {
        [SyncVar] public float c;
    }

    public class GenericNetworkBehaviorSyncvarNoMiddleTest : ClientServerSetup<With0>
    {
        [UnityTest]
        public IEnumerator CanSyncValues()
        {
            this.serverComponent.a = 10;
            this.serverComponent.b = 20;
            this.serverComponent.c = 12.5f;
            yield return new WaitForSeconds(0.2f);

            Assert.That(this.clientComponent.a, Is.EqualTo(10));
            Assert.That(this.clientComponent.b, Is.EqualTo(20));
            Assert.That(this.clientComponent.c, Is.EqualTo(12.5f));
        }
    }
}

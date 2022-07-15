using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class BehaviourWithSyncVarOnServerEvent : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnHealthChanged), invokeHookOnServer = true)]
        public int health;

        public event Action<int, int> OnHealthChanged;
    }

    public class BehaviourWithSyncVarOnServerMethod : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnHealthChanged), invokeHookOnServer = true)]
        public int health;

        public int called = 0;

        public void OnHealthChanged(int oldValue, int newValue)
        {
            if (!this.IsServer) return;

            this.called++;
        }
    }

    public class SyncVarFireHookEventOnServerTests : ClientServerSetup<BehaviourWithSyncVarOnServerEvent>
    {
        [UnityTest]
        public IEnumerator SyncVarHookEventIsCalledOnServer()
        {
            const int SValue = 10;
            const int CValue = 0;
            int oldValue = default;
            int newValue = default;
            var called = 0;

            this.serverComponent.OnHealthChanged += (a, b) =>
            {
                oldValue = a;
                newValue = b;
                called++;
            };

            this.serverComponent.health = SValue;

            yield return new WaitUntil(() => called > 0);

            Assert.That(called, Is.EqualTo(1));
            Assert.That(oldValue, Is.EqualTo(CValue));
            Assert.That(newValue, Is.EqualTo(SValue));

        }
    }

    public class SyncVarFireHookMethodOnServerTests : ClientServerSetup<BehaviourWithSyncVarOnServerMethod>
    {
        [UnityTest]
        public IEnumerator SyncVarHookMethodIsCalledOnServer()
        {
            const int SValue = 10;
            var oldValue = this.serverComponent.health;
            int newValue = default;

            this.serverComponent.health = SValue;

            yield return new WaitUntil(() => oldValue == newValue);

            newValue = this.serverComponent.health;

            Assert.That(this.serverComponent.called, Is.EqualTo(1));
            Assert.That(oldValue, Is.Not.EqualTo(this.serverComponent.health));
            Assert.That(newValue, Is.EqualTo(this.serverComponent.health));
        }
    }
}

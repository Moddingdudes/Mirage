using System;
using System.Collections;
using Mirage.Collections;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer.Generics
{
    public struct MyStruct<T>
    {
        public T value;
    }
    public struct MyHolder
    {
        public MyStruct<int> inner;
    }
    public class WithGenericInstances_Behaviour : NetworkBehaviour
    {
        public event Action<int> structParam;
        public event Action<int> holderParam;
        public event Action<MyStruct<int>, MyStruct<int>> syncEvent;
        public event Action<int, int> onSyncHook;

        public readonly SyncList<MyStruct<int>> myList = new SyncList<MyStruct<int>>();
        [SyncVar]
        public MyStruct<int> myVar;

        [SyncVar(hook = nameof(syncHook))]
        public MyStruct<int> varWithHook;

        private void syncHook(MyStruct<int> _old, MyStruct<int> _new) => onSyncHook?.Invoke(_old.value, _new.value);

        [SyncVar(hook = nameof(syncEvent))]
        public MyStruct<int> varWithEvent;

        [ClientRpc]
        public void MyRpc(MyStruct<int> value)
        {
            structParam.Invoke(value.value);
        }

        [ClientRpc]
        public void MyRpcHolder(MyHolder value)
        {
            holderParam.Invoke(value.inner.value);
        }
    }

    public class WithGenericInstances : ClientServerSetup<WithGenericInstances_Behaviour>
    {
        [UnityTest]
        public IEnumerator SyncListSendsValues()
        {
            const int num1 = 32;
            const int num2 = 48;
            this.serverComponent.myList.Add(new MyStruct<int> { value = num1 });
            this.serverComponent.myList.Add(new MyStruct<int> { value = num2 });

            yield return new WaitForSeconds(0.4f);

            Assert.That(this.clientComponent.myList.Count, Is.EqualTo(2));
            Assert.That(this.clientComponent.myList[0].value, Is.EqualTo(num1));
            Assert.That(this.clientComponent.myList[1].value, Is.EqualTo(num2));

            this.serverComponent.myList.Remove(new MyStruct<int> { value = num1 });

            yield return new WaitForSeconds(0.4f);

            Assert.That(this.clientComponent.myList.Count, Is.EqualTo(1));
            Assert.That(this.clientComponent.myList[0].value, Is.EqualTo(num2));
        }

        [UnityTest]
        public IEnumerator CanCallServerRpc()
        {
            const int num = 32;
            var sub = Substitute.For<Action<int>>();
            this.clientComponent.structParam += sub;
            this.serverComponent.MyRpc(new MyStruct<int> { value = num });

            yield return null;
            yield return null;

            sub.Received(1).Invoke(num);
        }

        [UnityTest]
        public IEnumerator CanCallServerRpcWithHolder()
        {
            const int num = 32;
            var sub = Substitute.For<Action<int>>();
            this.clientComponent.holderParam += sub;
            this.serverComponent.MyRpcHolder(new MyHolder
            {
                inner = new MyStruct<int>
                {
                    value = num
                }
            });

            yield return null;
            yield return null;

            sub.Received(1).Invoke(num);
        }

        [UnityTest]
        public IEnumerator CanSyncVar()
        {
            const int num = 32;
            this.serverComponent.myVar = new MyStruct<int> { value = num };

            yield return new WaitForSeconds(0.4f);

            Assert.That(this.clientComponent.myVar.value, Is.EqualTo(num));
        }

        [UnityTest]
        public IEnumerator CanSyncVarWithHook()
        {
            const int num = 32;
            var sub = Substitute.For<Action<int, int>>();
            this.clientComponent.onSyncHook += sub;
            this.serverComponent.varWithHook = new MyStruct<int> { value = num };

            yield return new WaitForSeconds(0.4f);

            Assert.That(this.clientComponent.varWithHook.value, Is.EqualTo(num));
            sub.Received(1).Invoke(0, num);
        }

        [UnityTest]
        public IEnumerator CanSyncVarWithEvent()
        {
            const int num = 32;
            var sub = Substitute.For<Action<MyStruct<int>, MyStruct<int>>>();
            this.clientComponent.syncEvent += sub;
            this.serverComponent.varWithEvent = new MyStruct<int> { value = num };

            yield return new WaitForSeconds(0.4f);

            Assert.That(this.clientComponent.varWithEvent.value, Is.EqualTo(num));
            sub.Received(1).Invoke(new MyStruct<int>(), new MyStruct<int> { value = num });
        }
    }
}

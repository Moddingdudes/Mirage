using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.ClientServer
{
    public class SampleBehaviorWithRpc : NetworkBehaviour
    {
        public event Action<NetworkIdentity> onSendNetworkIdentityCalled;
        public event Action<GameObject> onSendGameObjectCalled;
        public event Action<NetworkBehaviour> onSendNetworkBehaviourCalled;
        public event Action<SampleBehaviorWithRpc> onSendNetworkBehaviourDerivedCalled;
        public event Action<Weaver.Extra.SomeData> onSendTypeFromAnotherAssemblyCalled;
        public event Action<int, INetworkPlayer> onWithSenderCalled;
        public event Action<INetworkPlayer, int> onWithSenderInDifferentOrderCalled;

        [ClientRpc]
        public void SendNetworkIdentity(NetworkIdentity value)
        {
            onSendNetworkIdentityCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendGameObject(GameObject value)
        {
            onSendGameObjectCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendNetworkBehaviour(NetworkBehaviour value)
        {
            onSendNetworkBehaviourCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendNetworkBehaviourDerived(SampleBehaviorWithRpc value)
        {
            onSendNetworkBehaviourDerivedCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendNetworkIdentityToServer(NetworkIdentity value)
        {
            onSendNetworkIdentityCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendGameObjectToServer(GameObject value)
        {
            onSendGameObjectCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendNetworkBehaviourToServer(NetworkBehaviour value)
        {
            onSendNetworkBehaviourCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendNetworkBehaviourDerivedToServer(SampleBehaviorWithRpc value)
        {
            onSendNetworkBehaviourDerivedCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendTypeFromAnotherAssembly(Weaver.Extra.SomeData someData)
        {
            onSendTypeFromAnotherAssemblyCalled?.Invoke(someData);
        }

        [ServerRpc(requireAuthority = false)]
        public void WithSender(int myNumber, INetworkPlayer sender = null)
        {
            onWithSenderCalled?.Invoke(myNumber, sender);
        }

        [ServerRpc(requireAuthority = false)]
        public void WithSenderInDifferentOrder(INetworkPlayer sender, int myNumber)
        {
            onWithSenderInDifferentOrderCalled?.Invoke(sender, myNumber);
        }
    }

    public class NetworkBehaviorRPCTest : ClientServerSetup<SampleBehaviorWithRpc>
    {
        [UnityTest]
        public IEnumerator SendNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.clientComponent.onSendNetworkIdentityCalled += callback;

            this.serverComponent.SendNetworkIdentity(this.serverIdentity);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.clientIdentity);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehavior() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.clientComponent.onSendNetworkBehaviourCalled += callback;

            this.serverComponent.SendNetworkBehaviour(this.serverComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.clientComponent);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehaviorChild() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<SampleBehaviorWithRpc>>();
            this.clientComponent.onSendNetworkBehaviourDerivedCalled += callback;

            this.serverComponent.SendNetworkBehaviourDerived(this.serverComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.clientComponent);
        });

        [UnityTest]
        public IEnumerator SendGameObject() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendGameObjectCalled += callback;

            this.serverComponent.SendGameObject(this.serverPlayerGO);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.clientPlayerGO);
        });

        [Test]
        public void SendInvalidGO()
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendGameObjectCalled += callback;

            // this object does not have a NI, so this should error out
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverComponent.SendGameObject(this.serverGo);
            });
        }

        [UnityTest]
        public IEnumerator SendNullNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.clientComponent.onSendNetworkIdentityCalled += callback;

            this.serverComponent.SendNetworkIdentity(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNullNetworkBehavior() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.clientComponent.onSendNetworkBehaviourCalled += callback;

            this.serverComponent.SendNetworkBehaviour(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNullNetworkBehaviorChild() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<SampleBehaviorWithRpc>>();
            this.clientComponent.onSendNetworkBehaviourDerivedCalled += callback;

            this.serverComponent.SendNetworkBehaviourDerived(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNullGameObject() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendGameObjectCalled += callback;

            this.serverComponent.SendGameObject(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(null);
        });


        [UnityTest]
        public IEnumerator SendNetworkIdentityToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.serverComponent.onSendNetworkIdentityCalled += callback;

            this.clientComponent.SendNetworkIdentityToServer(this.clientIdentity);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.serverIdentity);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehaviorToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.serverComponent.onSendNetworkBehaviourCalled += callback;

            this.clientComponent.SendNetworkBehaviourToServer(this.clientComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.serverComponent);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehaviorChildToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<SampleBehaviorWithRpc>>();
            this.serverComponent.onSendNetworkBehaviourDerivedCalled += callback;

            this.clientComponent.SendNetworkBehaviourDerivedToServer(this.clientComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.serverComponent);
        });

        [UnityTest]
        public IEnumerator SendTypeFromAnotherAssembly() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<Weaver.Extra.SomeData>>();
            this.clientComponent.onSendTypeFromAnotherAssemblyCalled += callback;

            var someData = new Weaver.Extra.SomeData { usefulNumber = 13 };
            this.serverComponent.SendTypeFromAnotherAssembly(someData);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(someData);
        });

        [UnityTest]
        public IEnumerator SendGameObjectToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.serverComponent.onSendGameObjectCalled += callback;

            this.clientComponent.SendGameObjectToServer(this.clientPlayerGO);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.serverPlayerGO);
        });

        [UnityTest]
        public IEnumerator WithSender() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<int, INetworkPlayer>>();
            this.serverComponent.onWithSenderCalled += callback;

            const int value = 10;
            this.clientComponent.WithSender(value);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(value, this.serverPlayer);
        });

        [UnityTest]
        public IEnumerator WithSenderInDifferentOrder() => UniTask.ToCoroutine(async () =>
      {
          var callback = Substitute.For<Action<INetworkPlayer, int>>();
          this.serverComponent.onWithSenderInDifferentOrderCalled += callback;

          const int value = 10;
          this.clientComponent.WithSenderInDifferentOrder(null, value);
          await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
          callback.Received().Invoke(this.serverPlayer, value);
      });



        [Test]
        public void SendInvalidGOToServer()
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.serverComponent.onSendGameObjectCalled += callback;

            // this object does not have a NI, so this should error out
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.clientComponent.SendGameObjectToServer(this.clientGo);
            });
        }
    }
}

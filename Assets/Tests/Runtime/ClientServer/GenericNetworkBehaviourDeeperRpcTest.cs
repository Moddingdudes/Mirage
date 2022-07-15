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
    public class GenericBehaviourWithRpcDeeperBase<T> : NetworkBehaviour where T : NetworkBehaviour
    {
        public event Action<NetworkIdentity> onSendNetworkIdentityCalled;
        public event Action<GameObject> onSendGameObjectCalled;
        public event Action<NetworkBehaviour> onSendNetworkBehaviourCalled;
        public event Action<GenericBehaviourWithRpcDeeperImplement> onSendNetworkBehaviourDerivedCalled;

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
        public void SendNetworkBehaviourDerived(GenericBehaviourWithRpcDeeperImplement value)
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
        public void SendNetworkBehaviourDerivedToServer(GenericBehaviourWithRpcDeeperImplement value)
        {
            onSendNetworkBehaviourDerivedCalled?.Invoke(value);
        }
    }

    public class GenericBehaviourWithRpcDeeperMiddle<T> : GenericBehaviourWithRpcDeeperBase<T> where T : NetworkBehaviour
    {
        public event Action<NetworkIdentity> onSendDeeperNetworkIdentityCalled;
        public event Action<GameObject> onSendDeeperGameObjectCalled;
        public event Action<NetworkBehaviour> onSendDeeperNetworkBehaviourCalled;
        public event Action<GenericBehaviourWithRpcDeeperImplement> onSendDeeperNetworkBehaviourDerivedCalled;

        [ClientRpc]
        public void SendDeeperNetworkIdentity(NetworkIdentity value)
        {
            onSendDeeperNetworkIdentityCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendDeeperGameObject(GameObject value)
        {
            onSendDeeperGameObjectCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendDeeperNetworkBehaviour(NetworkBehaviour value)
        {
            onSendDeeperNetworkBehaviourCalled?.Invoke(value);
        }

        [ClientRpc]
        public void SendDeeperNetworkBehaviourDerived(GenericBehaviourWithRpcDeeperImplement value)
        {
            onSendDeeperNetworkBehaviourDerivedCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendDeeperNetworkIdentityToServer(NetworkIdentity value)
        {
            onSendDeeperNetworkIdentityCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendDeeperGameObjectToServer(GameObject value)
        {
            onSendDeeperGameObjectCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendDeeperNetworkBehaviourToServer(NetworkBehaviour value)
        {
            onSendDeeperNetworkBehaviourCalled?.Invoke(value);
        }

        [ServerRpc]
        public void SendDeeperNetworkBehaviourDerivedToServer(GenericBehaviourWithRpcDeeperImplement value)
        {
            onSendDeeperNetworkBehaviourDerivedCalled?.Invoke(value);
        }
    }

    public class GenericBehaviourWithRpcDeeperImplement : GenericBehaviourWithRpcDeeperMiddle<GenericBehaviourWithRpcDeeperImplement> { }

    public class GenericNetworkBehaviourDeeperRpcTests : ClientServerSetup<GenericBehaviourWithRpcDeeperImplement>
    {
        [UnityTest]
        public IEnumerator SendNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.clientComponent.onSendNetworkIdentityCalled += callback;

            this.serverComponent.SendNetworkIdentity(this.serverIdentity);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientIdentity);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehavior() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.clientComponent.onSendNetworkBehaviourCalled += callback;

            this.serverComponent.SendNetworkBehaviour(this.serverComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientComponent);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehaviorChild() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GenericBehaviourWithRpcDeeperImplement>>();
            this.clientComponent.onSendNetworkBehaviourDerivedCalled += callback;

            this.serverComponent.SendNetworkBehaviourDerived(this.serverComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientComponent);
        });

        [UnityTest]
        public IEnumerator SendGameObject() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendGameObjectCalled += callback;

            this.serverComponent.SendGameObject(this.serverPlayerGO);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
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
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNullNetworkBehavior() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.clientComponent.onSendNetworkBehaviourCalled += callback;

            this.serverComponent.SendNetworkBehaviour(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNullNetworkBehaviorChild() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GenericBehaviourWithRpcDeeperImplement>>();
            this.clientComponent.onSendNetworkBehaviourDerivedCalled += callback;

            this.serverComponent.SendNetworkBehaviourDerived(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNullGameObject() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendGameObjectCalled += callback;

            this.serverComponent.SendGameObject(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendNetworkIdentityToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.serverComponent.onSendNetworkIdentityCalled += callback;

            this.clientComponent.SendNetworkIdentityToServer(this.clientIdentity);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.serverIdentity);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehaviorToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.serverComponent.onSendNetworkBehaviourCalled += callback;

            this.clientComponent.SendNetworkBehaviourToServer(this.clientComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.serverComponent);
        });

        [UnityTest]
        public IEnumerator SendNetworkBehaviorChildToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GenericBehaviourWithRpcDeeperImplement>>();
            this.serverComponent.onSendNetworkBehaviourDerivedCalled += callback;

            this.clientComponent.SendNetworkBehaviourDerivedToServer(this.clientComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.serverComponent);
        });

        [UnityTest]
        public IEnumerator SendGameObjectToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.serverComponent.onSendGameObjectCalled += callback;

            this.clientComponent.SendGameObjectToServer(this.clientPlayerGO);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.serverPlayerGO);
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

        [UnityTest]
        public IEnumerator SendDeeperNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.clientComponent.onSendDeeperNetworkIdentityCalled += callback;

            this.serverComponent.SendDeeperNetworkIdentity(this.serverIdentity);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientIdentity);
        });

        [UnityTest]
        public IEnumerator SendDeeperNetworkBehavior() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.clientComponent.onSendDeeperNetworkBehaviourCalled += callback;

            this.serverComponent.SendDeeperNetworkBehaviour(this.serverComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientComponent);
        });

        [UnityTest]
        public IEnumerator SendDeeperNetworkBehaviorChild() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GenericBehaviourWithRpcDeeperImplement>>();
            this.clientComponent.onSendDeeperNetworkBehaviourDerivedCalled += callback;

            this.serverComponent.SendDeeperNetworkBehaviourDerived(this.serverComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientComponent);
        });

        [UnityTest]
        public IEnumerator SendDeeperGameObject() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendDeeperGameObjectCalled += callback;

            this.serverComponent.SendDeeperGameObject(this.serverPlayerGO);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.clientPlayerGO);
        });

        [Test]
        public void SendDeeperInvalidGO()
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendDeeperGameObjectCalled += callback;

            // this object does not have a NI, so this should error out
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.serverComponent.SendDeeperGameObject(this.serverGo);
            });
        }

        [UnityTest]
        public IEnumerator SendDeeperNullNetworkIdentity() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.clientComponent.onSendDeeperNetworkIdentityCalled += callback;

            this.serverComponent.SendDeeperNetworkIdentity(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendDeeperNullNetworkBehavior() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.clientComponent.onSendDeeperNetworkBehaviourCalled += callback;

            this.serverComponent.SendDeeperNetworkBehaviour(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendDeeperNullNetworkBehaviorChild() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GenericBehaviourWithRpcDeeperImplement>>();
            this.clientComponent.onSendDeeperNetworkBehaviourDerivedCalled += callback;

            this.serverComponent.SendDeeperNetworkBehaviourDerived(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendDeeperNullGameObject() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.clientComponent.onSendDeeperGameObjectCalled += callback;

            this.serverComponent.SendDeeperGameObject(null);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(null);
        });

        [UnityTest]
        public IEnumerator SendDeeperNetworkIdentityToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkIdentity>>();
            this.serverComponent.onSendDeeperNetworkIdentityCalled += callback;

            this.clientComponent.SendDeeperNetworkIdentityToServer(this.clientIdentity);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any());
            callback.Received().Invoke(this.serverIdentity);
        });

        [UnityTest]
        public IEnumerator SendDeeperNetworkBehaviorToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<NetworkBehaviour>>();
            this.serverComponent.onSendDeeperNetworkBehaviourCalled += callback;

            this.clientComponent.SendDeeperNetworkBehaviourToServer(this.clientComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2));
            callback.Received().Invoke(this.serverComponent);
        });

        [UnityTest]
        public IEnumerator SendDeeperNetworkBehaviorChildToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GenericBehaviourWithRpcDeeperImplement>>();
            this.serverComponent.onSendDeeperNetworkBehaviourDerivedCalled += callback;

            this.clientComponent.SendDeeperNetworkBehaviourDerivedToServer(this.clientComponent);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.serverComponent);
        });

        [UnityTest]
        public IEnumerator SendDeeperGameObjectToServer() => UniTask.ToCoroutine(async () =>
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.serverComponent.onSendDeeperGameObjectCalled += callback;

            this.clientComponent.SendDeeperGameObjectToServer(this.clientPlayerGO);
            await UniTask.WaitUntil(() => callback.ReceivedCalls().Any()).Timeout(TimeSpan.FromSeconds(2)); ;
            callback.Received().Invoke(this.serverPlayerGO);
        });

        [Test]
        public void SendDeeperInvalidGOToServer()
        {
            var callback = Substitute.For<Action<GameObject>>();
            this.serverComponent.onSendDeeperGameObjectCalled += callback;

            // this object does not have a NI, so this should error out
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.clientComponent.SendDeeperGameObjectToServer(this.clientGo);
            });
        }
    }
}

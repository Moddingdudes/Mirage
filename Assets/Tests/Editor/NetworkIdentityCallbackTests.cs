using System;
using System.Collections.Generic;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using static Mirage.Tests.LocalConnections;
using Object = UnityEngine.Object;

namespace Mirage.Tests
{
    public class NetworkIdentityCallbackTests
    {
        #region test components

        private class CheckObserverExceptionNetworkBehaviour : NetworkVisibility
        {
            public int called;
            public INetworkPlayer valuePassed;
            public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize) { }
            public override bool OnCheckObserver(INetworkPlayer player)
            {
                ++this.called;
                this.valuePassed = player;
                throw new Exception("some exception");
            }
        }

        private class CheckObserverTrueNetworkBehaviour : NetworkVisibility
        {
            public int called;
            public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize) { }
            public override bool OnCheckObserver(INetworkPlayer player)
            {
                ++this.called;
                return true;
            }
        }

        private class CheckObserverFalseNetworkBehaviour : NetworkVisibility
        {
            public int called;
            public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize) { }
            public override bool OnCheckObserver(INetworkPlayer player)
            {
                ++this.called;
                return false;
            }
        }

        private class SerializeTest1NetworkBehaviour : NetworkBehaviour
        {
            public int value;
            public override bool OnSerialize(NetworkWriter writer, bool initialState)
            {
                writer.WriteInt32(this.value);
                return true;
            }
            public override void OnDeserialize(NetworkReader reader, bool initialState)
            {
                this.value = reader.ReadInt32();
            }
        }

        private class SerializeTest2NetworkBehaviour : NetworkBehaviour
        {
            public string value;
            public override bool OnSerialize(NetworkWriter writer, bool initialState)
            {
                writer.WriteString(this.value);
                return true;
            }
            public override void OnDeserialize(NetworkReader reader, bool initialState)
            {
                this.value = reader.ReadString();
            }
        }

        private class SerializeExceptionNetworkBehaviour : NetworkBehaviour
        {
            public override bool OnSerialize(NetworkWriter writer, bool initialState)
            {
                throw new Exception("some exception");
            }
            public override void OnDeserialize(NetworkReader reader, bool initialState)
            {
                throw new Exception("some exception");
            }
        }

        private class SerializeMismatchNetworkBehaviour : NetworkBehaviour
        {
            public int value;
            public override bool OnSerialize(NetworkWriter writer, bool initialState)
            {
                writer.WriteInt32(this.value);
                // one too many
                writer.WriteInt32(this.value);
                return true;
            }
            public override void OnDeserialize(NetworkReader reader, bool initialState)
            {
                this.value = reader.ReadInt32();
            }
        }

        private class RebuildObserversNetworkBehaviour : NetworkVisibility
        {
            public INetworkPlayer observer;
            public override bool OnCheckObserver(INetworkPlayer player) { return true; }
            public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize)
            {
                observers.Add(this.observer);
            }
        }

        private class RebuildEmptyObserversNetworkBehaviour : NetworkVisibility
        {
            public override bool OnCheckObserver(INetworkPlayer player) { return true; }
            public override void OnRebuildObservers(HashSet<INetworkPlayer> observers, bool initialize) { }
        }

        #endregion

        private GameObject gameObject;
        private NetworkIdentity identity;
        private NetworkServer server;
        private ServerObjectManager serverObjectManager;
        private GameObject networkServerGameObject;
        private INetworkPlayer player1;
        private INetworkPlayer player2;

        [SetUp]
        public void SetUp()
        {
            this.networkServerGameObject = new GameObject();
            this.server = this.networkServerGameObject.AddComponent<NetworkServer>();
            this.serverObjectManager = this.networkServerGameObject.AddComponent<ServerObjectManager>();
            this.serverObjectManager.Server = this.server;
            this.networkServerGameObject.AddComponent<NetworkClient>();

            this.gameObject = new GameObject($"Test go {TestContext.CurrentContext.Test.Name}");
            this.identity = this.gameObject.AddComponent<NetworkIdentity>();
            this.identity.Server = this.server;
            this.identity.ServerObjectManager = this.serverObjectManager;

            this.player1 = Substitute.For<INetworkPlayer>();
            this.player2 = Substitute.For<INetworkPlayer>();
        }

        [TearDown]
        public void TearDown()
        {
            // set isServer is false. otherwise Destroy instead of
            // DestroyImmediate is called internally, giving an error in Editor
            Object.DestroyImmediate(this.gameObject);
            Object.DestroyImmediate(this.networkServerGameObject);
        }

        // A Test behaves as an ordinary method
        [Test]
        public void OnStartServerTest()
        {
            // lets add a component to check OnStartserver
            var func1 = Substitute.For<UnityAction>();
            var func2 = Substitute.For<UnityAction>();

            this.identity.OnStartServer.AddListener(func1);
            this.identity.OnStartServer.AddListener(func2);

            this.identity.StartServer();

            func1.Received(1).Invoke();
            func2.Received(1).Invoke();
        }

        [Test]
        public void GetSetPrefabHash()
        {
            // assign a guid
            var hash = 123456789;
            this.identity.PrefabHash = hash;

            // did it work?
            Assert.That(this.identity.PrefabHash, Is.EqualTo(hash));
        }

        [Test]
        public void SetPrefabHash_GivesErrorIfOneExists()
        {
            var hash1 = "Assets/Prefab/myPrefab.asset".GetStableHashCode();
            this.identity.PrefabHash = hash1;

            // assign a guid
            var hash2 = "Assets/Prefab/myPrefab2.asset".GetStableHashCode();
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                this.identity.PrefabHash = hash2;
            });

            Assert.That(exception.Message, Is.EqualTo($"Cannot set PrefabHash on NetworkIdentity '{this.identity.name}' because it already had an PrefabHash. Current PrefabHash is '{hash1}', attempted new PrefabHash is '{hash2}'."));
            // guid was changed
            Assert.That(this.identity.PrefabHash, Is.EqualTo(hash1));
        }

        [Test]
        public void SetPrefabHash_GivesErrorForEmptyGuid()
        {
            var hash1 = "Assets/Prefab/myPrefab.asset".GetStableHashCode();
            this.identity.PrefabHash = hash1;

            // assign a guid
            var hash2 = 0;
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                this.identity.PrefabHash = hash2;
            });

            Assert.That(exception.Message, Is.EqualTo($"Cannot set PrefabHash to an empty guid on NetworkIdentity '{this.identity.name}'. Old PrefabHash '{hash1}'."));
            // guid was NOT changed
            Assert.That(this.identity.PrefabHash, Is.EqualTo(hash1));
        }
        [Test]
        public void SetPrefabHash_DoesNotGiveErrorIfBothOldAndNewAreEmpty()
        {
            Debug.Assert(this.identity.PrefabHash == 0, "PrefabHash needs to be empty at the start of this test");
            // assign a guid
            var hash2 = 0;
            // expect no errors
            this.identity.PrefabHash = hash2;

            // guid was still empty
            Assert.That(this.identity.PrefabHash, Is.EqualTo(0));
        }

        [Test]
        public void SetClientOwner()
        {
            // SetClientOwner
            (_, var original) = PipedConnections(Substitute.For<IMessageReceiver>(), Substitute.For<IMessageReceiver>());
            this.identity.SetClientOwner(original);
            Assert.That(this.identity.Owner, Is.EqualTo(original));
        }

        [Test]
        public void SetOverrideClientOwner()
        {
            // SetClientOwner
            (_, var original) = PipedConnections(Substitute.For<IMessageReceiver>(), Substitute.For<IMessageReceiver>());
            this.identity.SetClientOwner(original);

            // setting it when it's already set shouldn't overwrite the original
            (_, var overwrite) = PipedConnections(Substitute.For<IMessageReceiver>(), Substitute.For<IMessageReceiver>());
            // will log a warning
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.identity.SetClientOwner(overwrite);
            });

            Assert.That(this.identity.Owner, Is.EqualTo(original));
        }

        [Test]
        public void RemoveObserverInternal()
        {
            // call OnStartServer so that observers dict is created
            this.identity.StartServer();

            // add an observer connection
            var player = Substitute.For<INetworkPlayer>();
            this.identity.observers.Add(player);

            var player2 = Substitute.For<INetworkPlayer>();
            // RemoveObserverInternal with invalid connection should do nothing
            this.identity.RemoveObserverInternal(player2);
            Assert.That(this.identity.observers, Is.EquivalentTo(new[] { player }));

            // RemoveObserverInternal with existing connection should remove it
            this.identity.RemoveObserverInternal(player);
            Assert.That(this.identity.observers, Is.Empty);
        }

        [Test]
        public void AssignSceneID()
        {
            // OnValidate will have assigned a random sceneId of format 0x00000000FFFFFFFF
            // -> make sure that one was assigned, and that the left part was
            //    left empty for scene hash
            Assert.That(this.identity.SceneId, Is.Not.Zero);
            Assert.That(this.identity.SceneId & 0xFFFF_FFFF_0000_0000ul, Is.Zero);

            // make sure that OnValidate added it to sceneIds dict
            Assert.That(NetworkIdentityIdGenerator.sceneIds[(int)(this.identity.SceneId & 0x0000_0000_FFFF_FFFFul)], Is.Not.Null);
        }

        [Test]
        public void SetSceneIdSceneHashPartInternal()
        {
            // Awake will have assigned a random sceneId of format 0x00000000FFFFFFFF
            // -> make sure that one was assigned, and that the left part was
            //    left empty for scene hash
            Assert.That(this.identity.SceneId, Is.Not.Zero);
            Assert.That(this.identity.SceneId & 0xFFFF_FFFF_0000_0000ul, Is.Zero, "scene hash should start empty");
            var originalId = this.identity.SceneId;

            // set scene hash
            NetworkIdentityIdGenerator.SetSceneHash(this.identity);

            var newSceneId = this.identity.SceneId;
            var newID = newSceneId & 0x0000_0000_FFFF_FFFFul;
            var newHash = newSceneId & 0xFFFF_FFFF_0000_0000ul;

            // make sure that the right part is still the random sceneid
            Assert.That(newID, Is.EqualTo(originalId));

            // make sure that the left part is a scene hash now
            Assert.That(newHash, Is.Not.Zero);

            // calling it again should said the exact same hash again
            NetworkIdentityIdGenerator.SetSceneHash(this.identity);
            Assert.That(this.identity.SceneId, Is.EqualTo(newSceneId), "should be same value as first time it was called");
        }

        [Test]
        public void OnValidateSetupIDsSetsEmptyPrefabHashForSceneObject()
        {
            // OnValidate will have been called. make sure that PrefabHash was set
            // to 0 empty and not anything else, because this is a scene object
            Assert.That(this.identity.PrefabHash, Is.EqualTo(0));
        }

        [Test]
        public void OnStartServerCallsComponentsAndCatchesExceptions()
        {
            // make a mock delegate
            var func = Substitute.For<UnityAction>();

            // add it to the listener
            this.identity.OnStartServer.AddListener(func);

            // Since we are testing that exceptions are not swallowed,
            // when the mock is invoked, throw an exception 
            func
                .When(f => f.Invoke())
                .Do(f => { throw new Exception("Some exception"); });

            // Make sure that the exception is not swallowed
            Assert.Throws<Exception>(() =>
            {
                this.identity.StartServer();
            });

            // ask the mock if it got invoked
            // if the mock is not invoked,  then this fails
            // This is a type of assert
            func.Received().Invoke();
        }

        [Test]
        public void OnStartClientCallsComponentsAndCatchesExceptions()
        {
            // add component
            var func = Substitute.For<UnityAction>();
            this.identity.OnStartClient.AddListener(func);

            func
                .When(f => f.Invoke())
                .Do(f => { throw new Exception("Some exception"); });

            // make sure exceptions are not swallowed
            Assert.Throws<Exception>(() =>
            {
                this.identity.StartClient();
            });
            func.Received().Invoke();

            // we have checks to make sure that it's only called once.
            Assert.DoesNotThrow(() =>
            {
                this.identity.StartClient();
            });
            func.Received(1).Invoke();
        }

        [Test]
        public void OnAuthorityChangedCallsComponentsAndCatchesExceptions()
        {
            // add component
            var func = Substitute.For<UnityAction<bool>>();
            this.identity.OnAuthorityChanged.AddListener(func);

            func
                .When(f => f.Invoke(Arg.Any<bool>()))
                .Do(f => { throw new Exception("Some exception"); });

            // make sure exceptions are not swallowed
            Assert.Throws<Exception>(() =>
            {
                this.identity.CallStartAuthority();
            });
            func.Received(1).Invoke(Arg.Any<bool>());
        }

        [Test]
        public void NotifyAuthorityCallsOnStartStopAuthority()
        {
            var startAuth = 0;
            var stopAuth = 0;
            this.identity.OnAuthorityChanged.AddListener(auth =>
            {
                if (auth) startAuth++;
                else stopAuth++;
            });

            // set authority from false to true, which should call OnStartAuthority
            this.identity.HasAuthority = true;
            this.identity.NotifyAuthority();
            // shouldn't be touched
            Assert.That(this.identity.HasAuthority, Is.True);
            // start should be called
            Assert.That(startAuth, Is.EqualTo(1));
            Assert.That(stopAuth, Is.EqualTo(0));

            // set it to true again, should do nothing because already true
            this.identity.HasAuthority = true;
            this.identity.NotifyAuthority();
            // shouldn't be touched
            Assert.That(this.identity.HasAuthority, Is.True);
            // same as before
            Assert.That(startAuth, Is.EqualTo(1));
            Assert.That(stopAuth, Is.EqualTo(0));

            // set it to false, should call OnStopAuthority
            this.identity.HasAuthority = false;
            this.identity.NotifyAuthority();
            // shouldn't be touched
            Assert.That(this.identity.HasAuthority, Is.False);
            // same as before
            Assert.That(startAuth, Is.EqualTo(1));
            Assert.That(stopAuth, Is.EqualTo(1));

            // set it to false again, should do nothing because already false
            this.identity.HasAuthority = false;
            this.identity.NotifyAuthority();
            // shouldn't be touched
            Assert.That(this.identity.HasAuthority, Is.False);
            // same as before
            Assert.That(startAuth, Is.EqualTo(1));
            Assert.That(stopAuth, Is.EqualTo(1));
        }

        [Test]
        public void OnCheckObserverCatchesException()
        {
            // add component
            this.gameObject.AddComponent<CheckObserverExceptionNetworkBehaviour>();

            // should catch the exception internally and not throw it
            Assert.Throws<Exception>(() =>
            {
                this.identity.OnCheckObserver(this.player1);
            });
        }

        [Test]
        public void OnCheckObserverTrue()
        {
            // create a networkidentity with a component that returns true
            // result should still be true.
            var gameObjectTrue = new GameObject();
            var identityTrue = gameObjectTrue.AddComponent<NetworkIdentity>();
            var compTrue = gameObjectTrue.AddComponent<CheckObserverTrueNetworkBehaviour>();
            Assert.That(identityTrue.OnCheckObserver(this.player1), Is.True);
            Assert.That(compTrue.called, Is.EqualTo(1));
        }

        [Test]
        public void OnCheckObserverFalse()
        {
            // create a networkidentity with a component that returns true and
            // one component that returns false.
            // result should still be false if any one returns false.
            var gameObjectFalse = new GameObject();
            var identityFalse = gameObjectFalse.AddComponent<NetworkIdentity>();
            var compFalse = gameObjectFalse.AddComponent<CheckObserverFalseNetworkBehaviour>();
            Assert.That(identityFalse.OnCheckObserver(this.player1), Is.False);
            Assert.That(compFalse.called, Is.EqualTo(1));
        }

        [Test]
        public void OnSerializeAllSafely()
        {
            // create a networkidentity with our test components
            var comp1 = this.gameObject.AddComponent<SerializeTest1NetworkBehaviour>();
            var compExc = this.gameObject.AddComponent<SerializeExceptionNetworkBehaviour>();
            var comp2 = this.gameObject.AddComponent<SerializeTest2NetworkBehaviour>();

            // set some unique values to serialize
            comp1.value = 12345;
            comp1.syncMode = SyncMode.Observers;
            compExc.syncMode = SyncMode.Observers;
            comp2.value = "67890";
            comp2.syncMode = SyncMode.Owner;

            // serialize all
            var ownerWriter = new NetworkWriter(1300);
            var observersWriter = new NetworkWriter(1300);

            // serialize should propagate exceptions
            Assert.Throws<Exception>(() =>
            {
                this.identity.OnSerializeAll(true, ownerWriter, observersWriter);
            });
        }

        // OnSerializeAllSafely supports at max 64 components, because our
        // dirty mask is ulong and can only handle so many bits.
        [Test]
        public void NoMoreThan64Components()
        {
            // add byte.MaxValue+1 components
            for (var i = 0; i < byte.MaxValue + 1; ++i)
            {
                this.gameObject.AddComponent<SerializeTest1NetworkBehaviour>();
            }
            // ingore error from creating cache (has its own test)
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = this.identity.NetworkBehaviours;
            });
        }

        // OnDeserializeSafely should be able to detect and handle serialization
        // mismatches (= if compA writes 10 bytes but only reads 8 or 12, it
        // shouldn't break compB's serialization. otherwise we end up with
        // insane runtime errors like monsters that look like npcs. that's what
        // happened back in the day with UNET).
        [Test]
        public void OnDeserializeSafelyShouldDetectAndHandleDeSerializationMismatch()
        {
            // add components
            var comp1 = this.gameObject.AddComponent<SerializeTest1NetworkBehaviour>();
            this.gameObject.AddComponent<SerializeMismatchNetworkBehaviour>();
            var comp2 = this.gameObject.AddComponent<SerializeTest2NetworkBehaviour>();

            // set some unique values to serialize
            comp1.value = 12345;
            comp2.value = "67890";

            // serialize
            var ownerWriter = new NetworkWriter(1300);
            var observersWriter = new NetworkWriter(1300);
            this.identity.OnSerializeAll(true, ownerWriter, observersWriter);

            // reset component values
            comp1.value = 0;
            comp2.value = null;

            // deserialize all
            var reader = new NetworkReader();
            reader.Reset(ownerWriter.ToArraySegment());
            Assert.Throws<DeserializeFailedException>(() =>
            {
                this.identity.OnDeserializeAll(reader, true);
            });
            reader.Dispose();
        }

        [Test]
        public void OnStartLocalPlayer()
        {
            // add components
            var funcEx = Substitute.For<UnityAction>();
            var func = Substitute.For<UnityAction>();

            this.identity.OnStartLocalPlayer.AddListener(funcEx);
            this.identity.OnStartLocalPlayer.AddListener(func);

            funcEx
                .When(f => f.Invoke())
                .Do(f => { throw new Exception("Some exception"); });


            // make sure that comp.OnStartServer was called
            // the exception was caught and not thrown in here.
            Assert.Throws<Exception>(() =>
            {
                this.identity.StartLocalPlayer();
            });

            funcEx.Received(1).Invoke();
            //Due to the order the listeners are added the one without exception is never called
            func.Received(0).Invoke();

            // we have checks to make sure that it's only called once.
            // let's see if they work.
            Assert.DoesNotThrow(() =>
            {
                this.identity.StartLocalPlayer();
            });
            // same as before?
            funcEx.Received(1).Invoke();
            //Due to the order the listeners are added the one without exception is never called
            func.Received(0).Invoke();
        }

        [Test]
        public void OnStopClient()
        {
            var mockCallback = Substitute.For<UnityAction>();
            this.identity.OnStopClient.AddListener(mockCallback);

            this.identity.StopClient();

            mockCallback.Received().Invoke();
        }

        [Test]
        public void OnStopServer()
        {
            var mockCallback = Substitute.For<UnityAction>();
            this.identity.OnStopServer.AddListener(mockCallback);

            this.identity.StopServer();

            mockCallback.Received().Invoke();
        }

        [Test]
        public void OnStopServerEx()
        {
            var mockCallback = Substitute.For<UnityAction>();
            mockCallback
                .When(f => f.Invoke())
                .Do(f => { throw new Exception("Some exception"); });

            this.identity.OnStopServer.AddListener(mockCallback);

            Assert.Throws<Exception>(() =>
            {
                this.identity.StopServer();
            });
        }

        [Test]
        public void AddObserver()
        {
            this.identity.Server = this.server;

            // call OnStartServer so that observers dict is created
            this.identity.StartServer();

            // call AddObservers
            this.identity.AddObserver(this.player1);
            this.identity.AddObserver(this.player2);
            Assert.That(this.identity.observers, Is.EquivalentTo(new[] { this.player1, this.player2 }));

            // adding a duplicate connectionId shouldn't overwrite the original
            this.identity.AddObserver(this.player1);
            Assert.That(this.identity.observers, Is.EquivalentTo(new[] { this.player1, this.player2 }));
        }

        [Test]
        public void ClearObservers()
        {
            // call OnStartServer so that observers dict is created
            this.identity.StartServer();

            // add some observers
            this.identity.observers.Add(this.player1);
            this.identity.observers.Add(this.player2);

            // call ClearObservers
            this.identity.ClearObservers();
            Assert.That(this.identity.observers.Count, Is.EqualTo(0));
        }


        [Test]
        public void Reset()
        {
            // creates .observers and generates a netId
            this.identity.StartServer();
            this.identity.Owner = this.player1;
            this.identity.observers.Add(this.player1);

            // mark for reset and reset
            this.identity.NetworkReset();
            Assert.That(this.identity.NetId, Is.EqualTo(0));
            Assert.That(this.identity.Owner, Is.Null);
        }

        [Test]
        public void GetNewObservers()
        {
            // add components
            var comp = this.gameObject.AddComponent<RebuildObserversNetworkBehaviour>();
            comp.observer = this.player1;

            // get new observers
            var observers = new HashSet<INetworkPlayer>();
            var result = this.identity.GetNewObservers(observers, true);
            Assert.That(result, Is.True);
            Assert.That(observers.Count, Is.EqualTo(1));
            Assert.That(observers.Contains(comp.observer), Is.True);
        }

        [Test]
        public void GetNewObserversClearsHashSet()
        {
            // get new observers. no observer components so it should just clear
            // it and not do anything else
            var observers = new HashSet<INetworkPlayer>
            {
                this.player1
            };
            this.identity.GetNewObservers(observers, true);
            Assert.That(observers.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetNewObserversFalseIfNoComponents()
        {
            // get new observers. no observer components so it should be false
            var observers = new HashSet<INetworkPlayer>();
            var result = this.identity.GetNewObservers(observers, true);
            Assert.That(result, Is.False);
        }

        // RebuildObservers should always add the own ready connection
        // (if any). fixes https://github.com/vis2k/Mirror/issues/692
        [Test]
        public void RebuildObserversDoesNotAddPlayerIfNotReady()
        {
            // add at least one observers component, otherwise it will just add
            // all server connections
            this.gameObject.AddComponent<RebuildEmptyObserversNetworkBehaviour>();

            // add own player connection that isn't ready
            (_, var connection) = PipedConnections(Substitute.For<IMessageReceiver>(), Substitute.For<IMessageReceiver>());
            // set not ready (ready is default true now)
            connection.SceneIsReady = false;

            this.identity.Owner = connection;

            // call OnStartServer so that observers dict is created
            this.identity.StartServer();

            // rebuild shouldn't add own player because conn wasn't set ready
            this.identity.RebuildObservers(true);
            Assert.That(this.identity.observers, Does.Not.Contains(this.identity.Owner));
        }

        [Test]
        public void RebuildObserversAddsReadyConnectionsIfImplemented()
        {

            // add a proximity checker
            // one with a ready connection, one with no ready connection, one with null connection
            var comp = this.gameObject.AddComponent<RebuildObserversNetworkBehaviour>();
            comp.observer = Substitute.For<INetworkPlayer>();
            comp.observer.SceneIsReady.Returns(true);

            // rebuild observers should add all component's ready observers
            this.identity.RebuildObservers(true);
            Assert.That(this.identity.observers, Is.EquivalentTo(new[] { comp.observer }));
        }


        [Test]
        public void RebuildObserversDoesntAddNotReadyConnectionsIfImplemented()
        {
            // add a proximity checker
            // one with a ready connection, one with no ready connection, one with null connection
            var comp = this.gameObject.AddComponent<RebuildObserversNetworkBehaviour>();
            comp.observer = Substitute.For<INetworkPlayer>();
            comp.observer.SceneIsReady.Returns(false);

            // rebuild observers should add all component's ready observers
            this.identity.RebuildObservers(true);
            Assert.That(this.identity.observers, Is.Empty);
        }

        [Test]
        public void RebuildObserversAddsReadyServerConnectionsIfNotImplemented()
        {
            var readyConnection = Substitute.For<INetworkPlayer>();
            readyConnection.SceneIsReady.Returns(true);
            var notReadyConnection = Substitute.For<INetworkPlayer>();
            notReadyConnection.SceneIsReady.Returns(false);

            // add some server connections
            this.server.AddTestPlayer(readyConnection);
            this.server.AddTestPlayer(notReadyConnection);

            // rebuild observers should add all ready server connections
            // because no component implements OnRebuildObservers
            this.identity.RebuildObservers(true);
            Assert.That(this.identity.observers, Is.EquivalentTo(new[] { readyConnection }));
        }

    }
}

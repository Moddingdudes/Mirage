using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Mirage.Tests
{
    public class NetworkWorldTests
    {
        private NetworkWorld world;
        private Action<NetworkIdentity> spawnListener;
        private Action<NetworkIdentity> unspawnListener;
        private HashSet<uint> existingIds;

        [SetUp]
        public void SetUp()
        {
            this.world = new NetworkWorld();
            this.spawnListener = Substitute.For<Action<NetworkIdentity>>();
            this.unspawnListener = Substitute.For<Action<NetworkIdentity>>();
            this.world.onSpawn += this.spawnListener;
            this.world.onUnspawn += this.unspawnListener;
            this.existingIds = new HashSet<uint>();
        }

        private void AddValidIdentity(out uint id, out NetworkIdentity identity)
        {
            id = this.getValidId();

            identity = new GameObject("WorldTest").AddComponent<NetworkIdentity>();
            identity.NetId = id;
            this.world.AddIdentity(id, identity);
        }

        private uint getValidId()
        {
            uint id;
            do
            {
                id = (uint)Random.Range(1, 10000);
            }
            while (this.existingIds.Contains(id));

            this.existingIds.Add(id);
            return id;
        }

        [Test]
        public void StartsEmpty()
        {
            Assert.That(this.world.SpawnedIdentities.Count, Is.Zero);
        }

        [Test]
        public void TryGetReturnsFalseIfNotFound()
        {
            var id = this.getValidId();
            var found = this.world.TryGetIdentity(id, out var _);
            Assert.That(found, Is.False);
        }
        [Test]
        public void TryGetReturnsFalseIfNull()
        {
            this.AddValidIdentity(out var id, out var identity);

            Object.DestroyImmediate(identity);

            var found = this.world.TryGetIdentity(id, out var _);
            Assert.That(found, Is.False);
        }
        [Test]
        public void TryGetReturnsTrueIfFound()
        {
            this.AddValidIdentity(out var id, out var identity);

            var found = this.world.TryGetIdentity(id, out var _);
            Assert.That(found, Is.True);
        }

        [Test]
        public void AddToCollection()
        {
            this.AddValidIdentity(out var id, out var expected);

            var collection = this.world.SpawnedIdentities;

            this.world.TryGetIdentity(id, out var actual);

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CanAddManyObjects()
        {
            this.AddValidIdentity(out var id1, out var expected1);
            this.AddValidIdentity(out var id2, out var expected2);
            this.AddValidIdentity(out var id3, out var expected3);
            this.AddValidIdentity(out var id4, out var expected4);

            var collection = this.world.SpawnedIdentities;

            this.world.TryGetIdentity(id1, out var actual1);
            this.world.TryGetIdentity(id2, out var actual2);
            this.world.TryGetIdentity(id3, out var actual3);
            this.world.TryGetIdentity(id4, out var actual4);

            Assert.That(collection.Count, Is.EqualTo(4));
            Assert.That(actual1, Is.EqualTo(expected1));
            Assert.That(actual2, Is.EqualTo(expected2));
            Assert.That(actual3, Is.EqualTo(expected3));
            Assert.That(actual4, Is.EqualTo(expected4));
        }
        [Test]
        public void AddInvokesEvent()
        {
            this.AddValidIdentity(out var id, out var expected);

            this.spawnListener.Received(1).Invoke(expected);
        }
        [Test]
        public void AddInvokesEventOncePerAdd()
        {
            this.AddValidIdentity(out var id1, out var expected1);
            this.AddValidIdentity(out var id2, out var expected2);
            this.AddValidIdentity(out var id3, out var expected3);
            this.AddValidIdentity(out var id4, out var expected4);

            this.spawnListener.Received(1).Invoke(expected1);
            this.spawnListener.Received(1).Invoke(expected2);
            this.spawnListener.Received(1).Invoke(expected3);
            this.spawnListener.Received(1).Invoke(expected4);
        }
        [Test]
        public void AddThrowsIfIdentityIsNull()
        {
            var id = this.getValidId();

            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                this.world.AddIdentity(id, null);
            });

            var expected = new ArgumentNullException("identity");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));

            this.spawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }
        [Test]
        public void AddThrowsIfIdAlreadyInCollection()
        {
            this.AddValidIdentity(out var id, out var identity);

            var exception = Assert.Throws<ArgumentException>(() =>
            {
                this.world.AddIdentity(id, identity);
            });

            var expected = new ArgumentException("An Identity with same id already exists in network world", "netId");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));
        }
        [Test]
        public void AddThrowsIfIdIs0()
        {
            uint id = 0;
            var identity = new GameObject("WorldTest").AddComponent<NetworkIdentity>();
            identity.NetId = id;

            var exception = Assert.Throws<ArgumentException>(() =>
            {
                this.world.AddIdentity(id, identity);
            });

            var expected = new ArgumentException("id can not be zero", "netId");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));

            this.spawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }
        [Test]
        public void AddAssertsIfIdentityDoesNotHaveMatchingId()
        {
            var id1 = this.getValidId();
            var id2 = this.getValidId();

            var identity = new GameObject("WorldTest").AddComponent<NetworkIdentity>();
            identity.NetId = id1;


            var exception = Assert.Throws<ArgumentException>(() =>
            {
                this.world.AddIdentity(id2, identity);
            });

            var expected = new ArgumentException("NetworkIdentity did not have matching netId", "identity");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));

            this.spawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }

        [Test]
        public void RemoveFromCollectionUsingIdentity()
        {
            this.AddValidIdentity(out var id, out var identity);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(1));

            this.world.RemoveIdentity(identity);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(0));

            Assert.That(this.world.TryGetIdentity(id, out var identityOut), Is.False);
            Assert.That(identityOut, Is.EqualTo(null));
        }
        [Test]
        public void RemoveFromCollectionUsingNetId()
        {
            this.AddValidIdentity(out var id, out var identity);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(1));

            this.world.RemoveIdentity(id);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(0));

            Assert.That(this.world.TryGetIdentity(id, out var identityOut), Is.False);
            Assert.That(identityOut, Is.EqualTo(null));
        }
        [Test]
        public void RemoveOnlyRemovesCorrectItem()
        {
            this.AddValidIdentity(out var id1, out var identity1);
            this.AddValidIdentity(out var id2, out var identity2);
            this.AddValidIdentity(out var id3, out var identity3);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(3));

            this.world.RemoveIdentity(identity2);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(2));

            Assert.That(this.world.TryGetIdentity(id1, out var identityOut1), Is.True);
            Assert.That(this.world.TryGetIdentity(id2, out var identityOut2), Is.False);
            Assert.That(this.world.TryGetIdentity(id3, out var identityOut3), Is.True);
            Assert.That(identityOut1, Is.EqualTo(identity1));
            Assert.That(identityOut2, Is.EqualTo(null));
            Assert.That(identityOut3, Is.EqualTo(identity3));
        }
        [Test]
        public void RemoveInvokesEvent()
        {
            this.AddValidIdentity(out var id1, out var identity1);
            this.AddValidIdentity(out var id2, out var identity2);
            this.AddValidIdentity(out var id3, out var identity3);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(3));

            this.world.RemoveIdentity(identity3);
            this.unspawnListener.Received(1).Invoke(identity3);

            this.world.RemoveIdentity(id1);
            this.unspawnListener.Received(1).Invoke(identity1);

            this.world.RemoveIdentity(id2);
            this.unspawnListener.Received(1).Invoke(identity2);
        }

        [Test]
        public void RemoveThrowsIfIdIs0()
        {
            uint id = 0;

            var exception = Assert.Throws<ArgumentException>(() =>
            {
                this.world.RemoveIdentity(id);
            });

            var expected = new ArgumentException("id can not be zero", "netId");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));

            this.unspawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }

        [Test]
        public void ClearRemovesAllFromCollection()
        {
            this.AddValidIdentity(out var id1, out var identity1);
            this.AddValidIdentity(out var id2, out var identity2);
            this.AddValidIdentity(out var id3, out var identity3);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(3));

            this.world.ClearSpawnedObjects();
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(0));
        }
        [Test]
        public void ClearDoesNotInvokeEvent()
        {
            this.AddValidIdentity(out var id1, out var identity1);
            this.AddValidIdentity(out var id2, out var identity2);
            this.AddValidIdentity(out var id3, out var identity3);
            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(3));

            this.world.ClearSpawnedObjects();
            this.unspawnListener.DidNotReceiveWithAnyArgs().Invoke(default);
        }

        [Test]
        public void ClearDestoryedObjects()
        {
            this.AddValidIdentity(out var id1, out var identity1);
            this.AddValidIdentity(out var id2, out var identity2);
            this.AddValidIdentity(out var id3, out var identity3);
            this.AddValidIdentity(out var id4, out var nullIdentity);

            Object.DestroyImmediate(nullIdentity);

            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(4));

            this.world.RemoveDestroyedObjects();

            foreach (var identity in this.world.SpawnedIdentities)
            {
                Assert.That(identity != null);
            }

            Assert.That(this.world.SpawnedIdentities.Count, Is.EqualTo(3));
        }
    }
}

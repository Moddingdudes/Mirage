using System;
using System.Collections.Generic;
using Mirage.Collections;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests.Runtime
{
    [TestFixture]
    public class SyncSetTest
    {
        public class SyncSetString : SyncHashSet<string> { }

        private SyncSetString serverSyncSet;
        private SyncSetString clientSyncSet;

        [SetUp]
        public void SetUp()
        {
            this.serverSyncSet = new SyncSetString();
            this.clientSyncSet = new SyncSetString();

            // add some data to the list
            this.serverSyncSet.Add("Hello");
            this.serverSyncSet.Add("World");
            this.serverSyncSet.Add("!");
            SerializeHelper.SerializeAllTo(this.serverSyncSet, this.clientSyncSet);
        }

        [Test]
        public void TestInit()
        {
            Assert.That(this.serverSyncSet, Is.EquivalentTo(new[] { "Hello", "World", "!" }));
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "Hello", "World", "!" }));
        }

        [Test]
        public void ClearEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncSet.OnClear += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncSet, this.clientSyncSet);
            callback.Received().Invoke();
        }

        [Test]
        public void InsertEventOnSyncAll()
        {
            var callback = Substitute.For<Action<string>>();
            this.clientSyncSet.OnAdd += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncSet, this.clientSyncSet);

            callback.Received().Invoke("Hello");
            callback.Received().Invoke("World");
            callback.Received().Invoke("!");
        }

        [Test]
        public void ChangeEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncSet.OnChange += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncSet, this.clientSyncSet);
            callback.Received().Invoke();
        }


        [Test]
        public void TestAdd()
        {
            this.serverSyncSet.Add("yay");
            Assert.That(this.serverSyncSet.IsDirty, Is.True);
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "Hello", "World", "!", "yay" }));
            Assert.That(this.serverSyncSet.IsDirty, Is.False);
        }

        [Test]
        public void TestClear()
        {
            this.serverSyncSet.Clear();
            Assert.That(this.serverSyncSet.IsDirty, Is.True);
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new string[] { }));
            Assert.That(this.serverSyncSet.IsDirty, Is.False);
        }

        [Test]
        public void TestRemove()
        {
            this.serverSyncSet.Remove("World");
            Assert.That(this.serverSyncSet.IsDirty, Is.True);
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "Hello", "!" }));
            Assert.That(this.serverSyncSet.IsDirty, Is.False);
        }

        [Test]
        public void TestMultSync()
        {
            this.serverSyncSet.Add("1");
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            // add some delta and see if it applies
            this.serverSyncSet.Add("2");
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "Hello", "World", "!", "1", "2" }));
        }

        [Test]
        public void AddClientCallbackTest()
        {
            var callback = Substitute.For<Action<string>>();
            this.clientSyncSet.OnAdd += callback;
            this.serverSyncSet.Add("yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            callback.Received().Invoke("yay");
        }

        [Test]
        public void RemoveClientCallbackTest()
        {
            var callback = Substitute.For<Action<string>>();
            this.clientSyncSet.OnRemove += callback;
            this.serverSyncSet.Remove("World");
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            callback.Received().Invoke("World");
        }

        [Test]
        public void ClearClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncSet.OnClear += callback;
            this.serverSyncSet.Clear();
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            callback.Received().Invoke();
        }

        [Test]
        public void ChangeClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncSet.OnChange += callback;
            this.serverSyncSet.Add("1");
            this.serverSyncSet.Add("2");
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            callback.Received(1).Invoke();
        }

        [Test]
        public void CountTest()
        {
            Assert.That(this.serverSyncSet.Count, Is.EqualTo(3));
        }

        [Test]
        public void TestExceptWith()
        {
            this.serverSyncSet.ExceptWith(new[] { "World", "Hello" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "!" }));
        }

        [Test]
        public void TestExceptWithSelf()
        {
            this.serverSyncSet.ExceptWith(this.serverSyncSet);
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new string[] { }));
        }

        [Test]
        public void TestIntersectWith()
        {
            this.serverSyncSet.IntersectWith(new[] { "World", "Hello" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "World", "Hello" }));
        }

        [Test]
        public void TestIntersectWithSet()
        {
            this.serverSyncSet.IntersectWith(new HashSet<string> { "World", "Hello" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "World", "Hello" }));
        }

        [Test]
        public void TestIsProperSubsetOf()
        {
            Assert.That(this.clientSyncSet.IsProperSubsetOf(new[] { "World", "Hello", "!", "pepe" }));
        }

        [Test]
        public void TestIsProperSubsetOfSet()
        {
            Assert.That(this.clientSyncSet.IsProperSubsetOf(new HashSet<string> { "World", "Hello", "!", "pepe" }));
        }

        [Test]
        public void TestIsNotProperSubsetOf()
        {
            Assert.That(this.clientSyncSet.IsProperSubsetOf(new[] { "World", "!", "pepe" }), Is.False);
        }

        [Test]
        public void TestIsProperSuperSetOf()
        {
            Assert.That(this.clientSyncSet.IsProperSupersetOf(new[] { "World", "Hello" }));
        }

        [Test]
        public void TestIsSubsetOf()
        {
            Assert.That(this.clientSyncSet.IsSubsetOf(new[] { "World", "Hello", "!" }));
        }

        [Test]
        public void TestIsSupersetOf()
        {
            Assert.That(this.clientSyncSet.IsSupersetOf(new[] { "World", "Hello" }));
        }

        [Test]
        public void TestOverlaps()
        {
            Assert.That(this.clientSyncSet.Overlaps(new[] { "World", "my", "baby" }));
        }

        [Test]
        public void TestSetEquals()
        {
            Assert.That(this.clientSyncSet.SetEquals(new[] { "World", "Hello", "!" }));
        }

        [Test]
        public void TestSymmetricExceptWith()
        {
            this.serverSyncSet.SymmetricExceptWith(new HashSet<string> { "Hello", "is" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "World", "is", "!" }));
        }

        [Test]
        public void TestSymmetricExceptWithSelf()
        {
            this.serverSyncSet.SymmetricExceptWith(this.serverSyncSet);
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new string[] { }));
        }

        [Test]
        public void TestUnionWith()
        {
            this.serverSyncSet.UnionWith(new HashSet<string> { "Hello", "is" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "World", "Hello", "is", "!" }));
        }

        [Test]
        public void TestUnionWithSelf()
        {
            this.serverSyncSet.UnionWith(this.serverSyncSet);
            SerializeHelper.SerializeDeltaTo(this.serverSyncSet, this.clientSyncSet);
            Assert.That(this.clientSyncSet, Is.EquivalentTo(new[] { "World", "Hello", "!" }));
        }

        [Test]
        public void ReadOnlyTest()
        {
            Assert.That(this.serverSyncSet.IsReadOnly, Is.False);
            Assert.That(this.clientSyncSet.IsReadOnly, Is.True);
        }

        [Test]
        public void WritingToReadOnlyThrows()
        {
            Assert.Throws<InvalidOperationException>(() => { this.clientSyncSet.Add("5"); });
        }

        [Test]
        public void ObjectCanBeReusedAfterReset()
        {
            this.clientSyncSet.Reset();

            // make old client the host
            var hostList = this.clientSyncSet;
            var clientList2 = new SyncSetString();

            Assert.That(hostList.IsReadOnly, Is.False);

            // Check Add and Sync without errors
            hostList.Add("1");
            hostList.Add("2");
            hostList.Add("3");
            SerializeHelper.SerializeDeltaTo(hostList, clientList2);
        }

        [Test]
        public void ResetShouldSetReadOnlyToFalse()
        {
            this.clientSyncSet.Reset();

            Assert.That(this.clientSyncSet.IsReadOnly, Is.False);
        }

        [Test]
        public void ResetShouldClearChanges()
        {
            this.serverSyncSet.Reset();

            Assert.That(this.serverSyncSet.ChangeCount, Is.Zero);
        }

        [Test]
        public void ResetShouldClearItems()
        {
            this.serverSyncSet.Reset();

            Assert.That(this.serverSyncSet, Is.Empty);
        }
    }
}

using System;
using System.Collections.Generic;
using Mirage.Collections;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests.Runtime
{
    [TestFixture]
    public class SyncDictionaryTest
    {
        public class SyncDictionaryIntString : SyncDictionary<int, string>
        {
        }

        private SyncDictionaryIntString serverSyncDictionary;
        private SyncDictionaryIntString clientSyncDictionary;

        [SetUp]
        public void SetUp()
        {
            this.serverSyncDictionary = new SyncDictionaryIntString();
            this.clientSyncDictionary = new SyncDictionaryIntString();

            // add some data to the list
            this.serverSyncDictionary.Add(0, "Hello");
            this.serverSyncDictionary.Add(1, "World");
            this.serverSyncDictionary.Add(2, "!");
            SerializeHelper.SerializeAllTo(this.serverSyncDictionary, this.clientSyncDictionary);
        }

        [Test]
        public void TestInit()
        {
            var comparer = new Dictionary<int, string>
            {
                [0] = "Hello",
                [1] = "World",
                [2] = "!"
            };
            Assert.That(this.clientSyncDictionary[0], Is.EqualTo("Hello"));
            Assert.That(this.clientSyncDictionary, Is.EquivalentTo(comparer));
        }

        [Test]
        public void ClearEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncDictionary.OnClear += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke();
        }

        [Test]
        public void InsertEventOnSyncAll()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncDictionary.OnInsert += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncDictionary, this.clientSyncDictionary);

            callback.Received().Invoke(0, "Hello");
            callback.Received().Invoke(1, "World");
            callback.Received().Invoke(2, "!");
        }

        [Test]
        public void ChangeEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncDictionary.OnChange += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke();
        }

        [Test]
        public void TestAdd()
        {
            this.serverSyncDictionary.Add(4, "yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary.ContainsKey(4));
            Assert.That(this.clientSyncDictionary[4], Is.EqualTo("yay"));
        }

        [Test]
        public void TestClear()
        {
            this.serverSyncDictionary.Clear();
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.serverSyncDictionary, Is.EquivalentTo(new SyncDictionaryIntString()));
        }

        [Test]
        public void TestSet()
        {
            this.serverSyncDictionary[1] = "yay";
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary.ContainsKey(1));
            Assert.That(this.clientSyncDictionary[1], Is.EqualTo("yay"));
        }

        [Test]
        public void TestBareSet()
        {
            this.serverSyncDictionary[4] = "yay";
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary.ContainsKey(4));
            Assert.That(this.clientSyncDictionary[4], Is.EqualTo("yay"));
        }

        [Test]
        public void TestBareSetNull()
        {
            this.serverSyncDictionary[4] = null;
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary[4], Is.Null);
            Assert.That(this.clientSyncDictionary.ContainsKey(4));
        }

        [Test]
        public void TestConsecutiveSet()
        {
            this.serverSyncDictionary[1] = "yay";
            this.serverSyncDictionary[1] = "world";
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary[1], Is.EqualTo("world"));
        }

        [Test]
        public void TestNullSet()
        {
            this.serverSyncDictionary[1] = null;
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary.ContainsKey(1));
            Assert.That(this.clientSyncDictionary[1], Is.Null);
        }

        [Test]
        public void TestRemove()
        {
            this.serverSyncDictionary.Remove(1);
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(!this.clientSyncDictionary.ContainsKey(1));
        }

        [Test]
        public void TestMultSync()
        {
            this.serverSyncDictionary.Add(10, "1");
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            // add some delta and see if it applies
            this.serverSyncDictionary.Add(11, "2");
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary.ContainsKey(10));
            Assert.That(this.clientSyncDictionary[10], Is.EqualTo("1"));
            Assert.That(this.clientSyncDictionary.ContainsKey(11));
            Assert.That(this.clientSyncDictionary[11], Is.EqualTo("2"));
        }

        [Test]
        public void TestContains()
        {
            Assert.That(!this.clientSyncDictionary.Contains(new KeyValuePair<int, string>(2, "Hello")));
            this.serverSyncDictionary[2] = "Hello";
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            Assert.That(this.clientSyncDictionary.Contains(new KeyValuePair<int, string>(2, "Hello")));
        }

        [Test]
        public void AddClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncDictionary.OnInsert += callback;
            this.serverSyncDictionary.Add(3, "yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke(3, "yay");
        }

        [Test]
        public void AddServerCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.serverSyncDictionary.OnInsert += callback;
            this.serverSyncDictionary.Add(3, "yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke(3, "yay");
        }

        [Test]
        public void RemoveClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncDictionary.OnRemove += callback;
            this.serverSyncDictionary.Remove(1);
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke(1, "World");
        }

        [Test]
        public void ClearClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncDictionary.OnClear += callback;
            this.serverSyncDictionary.Clear();
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke();
        }

        [Test]
        public void ChangeClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncDictionary.OnChange += callback;
            this.serverSyncDictionary.Add(3, "1");
            this.serverSyncDictionary.Add(4, "1");
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received(1).Invoke();
        }

        [Test]
        public void SetClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string, string>>();
            this.clientSyncDictionary.OnSet += callback;
            this.serverSyncDictionary[0] = "yay";
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);
            callback.Received().Invoke(0, "Hello", "yay");
        }

        [Test]
        public void CountTest()
        {
            Assert.That(this.serverSyncDictionary.Count, Is.EqualTo(3));
        }

        [Test]
        public void CopyToTest()
        {
            var data = new KeyValuePair<int, string>[3];

            this.clientSyncDictionary.CopyTo(data, 0);

            Assert.That(data, Is.EquivalentTo(new[]
            {
                new KeyValuePair<int, string>(0, "Hello"),
                new KeyValuePair<int, string>(1, "World"),
                new KeyValuePair<int, string>(2, "!"),

            }));
        }

        [Test]
        public void CopyToOutOfRangeTest()
        {
            var data = new KeyValuePair<int, string>[3];

            Assert.Throws(typeof(ArgumentOutOfRangeException), delegate
            {
                this.clientSyncDictionary.CopyTo(data, -1);
            });
        }

        [Test]
        public void CopyToOutOfBoundsTest()
        {
            var data = new KeyValuePair<int, string>[3];

            Assert.Throws(typeof(ArgumentException), delegate
            {
                this.clientSyncDictionary.CopyTo(data, 2);
            });
        }

        [Test]
        public void TestRemovePair()
        {
            var data = new KeyValuePair<int, string>(0, "Hello");

            this.serverSyncDictionary.Remove(data);

            Assert.That(this.serverSyncDictionary, Is.EquivalentTo(new[]
            {
                new KeyValuePair<int, string>(1, "World"),
                new KeyValuePair<int, string>(2, "!"),

            }));
        }

        [Test]
        public void ReadOnlyTest()
        {
            Assert.That(this.serverSyncDictionary.IsReadOnly, Is.False);
            Assert.That(this.clientSyncDictionary.IsReadOnly, Is.True);
        }

        [Test]
        public void WritingToReadOnlyThrows()
        {
            Assert.Throws<InvalidOperationException>(() => this.clientSyncDictionary.Add(50, "fail"));
        }

        [Test]
        public void DirtyTest()
        {
            // Sync Delta to clear dirty
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);

            // nothing to send
            Assert.That(this.serverSyncDictionary.IsDirty, Is.False);

            // something has changed
            this.serverSyncDictionary.Add(15, "yay");
            Assert.That(this.serverSyncDictionary.IsDirty, Is.True);
            SerializeHelper.SerializeDeltaTo(this.serverSyncDictionary, this.clientSyncDictionary);

            // data has been flushed,  should go back to clear
            Assert.That(this.serverSyncDictionary.IsDirty, Is.False);
        }


        [Test]
        public void ObjectCanBeReusedAfterReset()
        {
            this.clientSyncDictionary.Reset();

            // make old client the host
            var hostList = this.clientSyncDictionary;
            var clientList2 = new SyncDictionaryIntString();

            Assert.That(hostList.IsReadOnly, Is.False);

            // Check Add and Sync without errors
            hostList.Add(30, "hello");
            hostList.Add(35, "world");
            SerializeHelper.SerializeDeltaTo(hostList, clientList2);
        }

        [Test]
        public void ResetShouldSetReadOnlyToFalse()
        {
            this.clientSyncDictionary.Reset();

            Assert.That(this.clientSyncDictionary.IsReadOnly, Is.False);
        }

        [Test]
        public void ResetShouldClearChanges()
        {
            this.serverSyncDictionary.Reset();

            Assert.That(this.serverSyncDictionary.ChangeCount, Is.Zero);
        }

        [Test]
        public void ResetShouldClearItems()
        {
            this.serverSyncDictionary.Reset();

            Assert.That(this.serverSyncDictionary, Is.Empty);
        }
    }
}

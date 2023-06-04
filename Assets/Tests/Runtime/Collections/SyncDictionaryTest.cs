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
            serverSyncDictionary = new SyncDictionaryIntString();
            clientSyncDictionary = new SyncDictionaryIntString();

            // add some data to the list
            serverSyncDictionary.Add(0, "Hello");
            serverSyncDictionary.Add(1, "World");
            serverSyncDictionary.Add(2, "!");
            SyncObjectHelper.SerializeAllTo(serverSyncDictionary, clientSyncDictionary);
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
            Assert.That(clientSyncDictionary[0], Is.EqualTo("Hello"));
            Assert.That(clientSyncDictionary, Is.EquivalentTo(comparer));
        }

        [Test]
        public void ClearEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            clientSyncDictionary.OnClear += callback;
            SyncObjectHelper.SerializeAllTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke();
        }

        [Test]
        public void InsertEventOnSyncAll()
        {
            var callback = Substitute.For<Action<int, string>>();
            clientSyncDictionary.OnInsert += callback;
            SyncObjectHelper.SerializeAllTo(serverSyncDictionary, clientSyncDictionary);

            callback.Received().Invoke(0, "Hello");
            callback.Received().Invoke(1, "World");
            callback.Received().Invoke(2, "!");
        }

        [Test]
        public void ChangeEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            clientSyncDictionary.OnChange += callback;
            SyncObjectHelper.SerializeAllTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke();
        }

        [Test]
        public void TestAdd()
        {
            serverSyncDictionary.Add(4, "yay");
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary.ContainsKey(4));
            Assert.That(clientSyncDictionary[4], Is.EqualTo("yay"));
        }

        [Test]
        public void TestClear()
        {
            serverSyncDictionary.Clear();
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(serverSyncDictionary, Is.EquivalentTo(new SyncDictionaryIntString()));
        }

        [Test]
        public void TestSet()
        {
            serverSyncDictionary[1] = "yay";
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary.ContainsKey(1));
            Assert.That(clientSyncDictionary[1], Is.EqualTo("yay"));
        }

        [Test]
        public void TestBareSet()
        {
            serverSyncDictionary[4] = "yay";
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary.ContainsKey(4));
            Assert.That(clientSyncDictionary[4], Is.EqualTo("yay"));
        }

        [Test]
        public void TestBareSetNull()
        {
            serverSyncDictionary[4] = null;
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary[4], Is.Null);
            Assert.That(clientSyncDictionary.ContainsKey(4));
        }

        [Test]
        public void TestConsecutiveSet()
        {
            serverSyncDictionary[1] = "yay";
            serverSyncDictionary[1] = "world";
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary[1], Is.EqualTo("world"));
        }

        [Test]
        public void TestNullSet()
        {
            serverSyncDictionary[1] = null;
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary.ContainsKey(1));
            Assert.That(clientSyncDictionary[1], Is.Null);
        }

        [Test]
        public void TestRemove()
        {
            serverSyncDictionary.Remove(1);
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(!clientSyncDictionary.ContainsKey(1));
        }

        [Test]
        public void TestMultSync()
        {
            serverSyncDictionary.Add(10, "1");
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            // add some delta and see if it applies
            serverSyncDictionary.Add(11, "2");
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary.ContainsKey(10));
            Assert.That(clientSyncDictionary[10], Is.EqualTo("1"));
            Assert.That(clientSyncDictionary.ContainsKey(11));
            Assert.That(clientSyncDictionary[11], Is.EqualTo("2"));
        }

        [Test]
        public void TestContains()
        {
            Assert.That(!clientSyncDictionary.Contains(new KeyValuePair<int, string>(2, "Hello")));
            serverSyncDictionary[2] = "Hello";
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            Assert.That(clientSyncDictionary.Contains(new KeyValuePair<int, string>(2, "Hello")));
        }

        [Test]
        public void AddClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            clientSyncDictionary.OnInsert += callback;
            serverSyncDictionary.Add(3, "yay");
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke(3, "yay");
        }

        [Test]
        public void AddServerCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            serverSyncDictionary.OnInsert += callback;
            serverSyncDictionary.Add(3, "yay");
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke(3, "yay");
        }

        [Test]
        public void RemoveClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            clientSyncDictionary.OnRemove += callback;
            serverSyncDictionary.Remove(1);
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke(1, "World");
        }

        [Test]
        public void ClearClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            clientSyncDictionary.OnClear += callback;
            serverSyncDictionary.Clear();
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke();
        }

        [Test]
        public void ChangeClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            clientSyncDictionary.OnChange += callback;
            serverSyncDictionary.Add(3, "1");
            serverSyncDictionary.Add(4, "1");
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received(1).Invoke();
        }

        [Test]
        public void SetClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string, string>>();
            clientSyncDictionary.OnSet += callback;
            serverSyncDictionary[0] = "yay";
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);
            callback.Received().Invoke(0, "Hello", "yay");
        }

        [Test]
        public void CountTest()
        {
            Assert.That(serverSyncDictionary.Count, Is.EqualTo(3));
        }

        [Test]
        public void CopyToTest()
        {
            var data = new KeyValuePair<int, string>[3];

            clientSyncDictionary.CopyTo(data, 0);

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
                clientSyncDictionary.CopyTo(data, -1);
            });
        }

        [Test]
        public void CopyToOutOfBoundsTest()
        {
            var data = new KeyValuePair<int, string>[3];

            Assert.Throws(typeof(ArgumentException), delegate
            {
                clientSyncDictionary.CopyTo(data, 2);
            });
        }

        [Test]
        public void TestRemovePair()
        {
            var data = new KeyValuePair<int, string>(0, "Hello");

            serverSyncDictionary.Remove(data);

            Assert.That(serverSyncDictionary, Is.EquivalentTo(new[]
            {
                new KeyValuePair<int, string>(1, "World"),
                new KeyValuePair<int, string>(2, "!"),

            }));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ReadOnlyTest(bool shouldSync)
        {
            var asObject = (ISyncObject)serverSyncDictionary;
            asObject.SetShouldSyncFrom(shouldSync);
            Assert.That(serverSyncDictionary.IsReadOnly, Is.EqualTo(!shouldSync));

            serverSyncDictionary.Reset();
            Assert.That(serverSyncDictionary.IsReadOnly, Is.EqualTo(false));
        }

        [Test]
        public void WritingToReadOnlyThrows()
        {
            var asObject = (ISyncObject)serverSyncDictionary;
            asObject.SetShouldSyncFrom(false);
            Assert.Throws<InvalidOperationException>(() =>
            {
                serverSyncDictionary.Add(5, "fail");
            });
        }

        [Test]
        public void DirtyTest()
        {
            // Sync Delta to clear dirty
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);

            // nothing to send
            Assert.That(serverSyncDictionary.IsDirty, Is.False);

            // something has changed
            serverSyncDictionary.Add(15, "yay");
            Assert.That(serverSyncDictionary.IsDirty, Is.True);
            SyncObjectHelper.SerializeDeltaTo(serverSyncDictionary, clientSyncDictionary);

            // data has been flushed,  should go back to clear
            Assert.That(serverSyncDictionary.IsDirty, Is.False);
        }


        [Test]
        public void ObjectCanBeReusedAfterReset()
        {
            clientSyncDictionary.Reset();

            // make old client the host
            var hostList = clientSyncDictionary;
            var clientList2 = new SyncDictionaryIntString();

            Assert.That(hostList.IsReadOnly, Is.False);

            // Check Add and Sync without errors
            hostList.Add(30, "hello");
            hostList.Add(35, "world");
            SyncObjectHelper.SerializeDeltaTo(hostList, clientList2);
        }

        [Test]
        public void ResetShouldSetReadOnlyToFalse()
        {
            clientSyncDictionary.Reset();

            Assert.That(clientSyncDictionary.IsReadOnly, Is.False);
        }

        [Test]
        public void ResetShouldClearChanges()
        {
            serverSyncDictionary.Reset();

            Assert.That(serverSyncDictionary.ChangeCount, Is.Zero);
        }

        [Test]
        public void ResetShouldClearItems()
        {
            serverSyncDictionary.Reset();

            Assert.That(serverSyncDictionary, Is.Empty);
        }
    }
}

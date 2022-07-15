using System;
using Mirage.Collections;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests.Runtime
{
    public static class SerializeHelper
    {
        private static readonly NetworkWriter writer = new NetworkWriter(1300);
        private static readonly NetworkReader reader = new NetworkReader();

        public static void SerializeAllTo<T>(T fromList, T toList) where T : ISyncObject
        {
            writer.Reset();
            fromList.OnSerializeAll(writer);

            reader.Reset(writer.ToArray());
            toList.OnDeserializeAll(reader);

            var writeLength = writer.ByteLength;
            var readLength = reader.BytePosition;
            Assert.That(writeLength == readLength, $"OnSerializeAll and OnDeserializeAll calls write the same amount of data\n    writeLength={writeLength}\n    readLength={readLength}");

        }

        public static void SerializeDeltaTo<T>(T fromList, T toList) where T : ISyncObject
        {
            writer.Reset();

            fromList.OnSerializeDelta(writer);
            reader.Reset(writer.ToArray());

            toList.OnDeserializeDelta(reader);
            fromList.Flush();

            var writeLength = writer.ByteLength;
            var readLength = reader.BytePosition;
            Assert.That(writeLength == readLength, $"OnSerializeDelta and OnDeserializeDelta calls write the same amount of data\n    writeLength={writeLength}\n    readLength={readLength}");
        }
    }
    [TestFixture]
    public class SyncListTest
    {
        private SyncList<string> serverSyncList;
        private SyncList<string> clientSyncList;



        [SetUp]
        public void SetUp()
        {
            this.serverSyncList = new SyncList<string>();
            this.clientSyncList = new SyncList<string>();

            // add some data to the list
            this.serverSyncList.Add("Hello");
            this.serverSyncList.Add("World");
            this.serverSyncList.Add("!");
            SerializeHelper.SerializeAllTo(this.serverSyncList, this.clientSyncList);
        }

        [Test]
        public void TestInit()
        {
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "World", "!" }));
        }

        [Test]
        public void ClearEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncList.OnClear += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke();
        }

        [Test]
        public void InsertEventOnSyncAll()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncList.OnInsert += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncList, this.clientSyncList);

            Received.InOrder(() =>
            {
                callback.Invoke(0, "Hello");
                callback.Invoke(1, "World");
                callback.Invoke(2, "!");
            });
        }

        [Test]
        public void ChangeEventOnSyncAll()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncList.OnChange += callback;
            SerializeHelper.SerializeAllTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke();
        }

        [Test]
        public void TestAdd()
        {
            this.serverSyncList.Add("yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "World", "!", "yay" }));
        }

        [Test]
        public void TestAddRange()
        {
            this.serverSyncList.AddRange(new[] { "One", "Two", "Three" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EqualTo(new[] { "Hello", "World", "!", "One", "Two", "Three" }));
        }

        [Test]
        public void TestClear()
        {
            this.serverSyncList.Clear();
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new string[] { }));
        }

        [Test]
        public void TestInsert()
        {
            this.serverSyncList.Insert(0, "yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "yay", "Hello", "World", "!" }));
        }

        [Test]
        public void TestInsertRange()
        {
            this.serverSyncList.InsertRange(1, new[] { "One", "Two", "Three" });
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EqualTo(new[] { "Hello", "One", "Two", "Three", "World", "!" }));
        }

        [Test]
        public void TestSet()
        {
            this.serverSyncList[1] = "yay";
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList[1], Is.EqualTo("yay"));
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "yay", "!" }));
        }

        [Test]
        public void TestSetNull()
        {
            this.serverSyncList[1] = null;
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList[1], Is.EqualTo(null));
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", null, "!" }));
            this.serverSyncList[1] = "yay";
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "yay", "!" }));
        }

        [Test]
        public void TestRemoveAll()
        {
            this.serverSyncList.RemoveAll(entry => entry.Contains("l"));
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "!" }));
        }

        [Test]
        public void TestRemoveAllNone()
        {
            this.serverSyncList.RemoveAll(entry => entry == "yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "World", "!" }));
        }

        [Test]
        public void TestRemoveAt()
        {
            this.serverSyncList.RemoveAt(1);
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "!" }));
        }

        [Test]
        public void TestRemove()
        {
            this.serverSyncList.Remove("World");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "!" }));
        }

        [Test]
        public void TestFindIndex()
        {
            var index = this.serverSyncList.FindIndex(entry => entry == "World");
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void TestFind()
        {
            var element = this.serverSyncList.Find(entry => entry == "World");
            Assert.That(element, Is.EqualTo("World"));
        }

        [Test]
        public void TestNoFind()
        {
            var nonexistent = this.serverSyncList.Find(entry => entry == "yay");
            Assert.That(nonexistent, Is.Null);
        }

        [Test]
        public void TestFindAll()
        {
            var results = this.serverSyncList.FindAll(entry => entry.Contains("l"));
            Assert.That(results, Is.EquivalentTo(new[] { "Hello", "World" }));
        }

        [Test]
        public void TestFindAllNonExistent()
        {
            var nonexistent = this.serverSyncList.FindAll(entry => entry == "yay");
            Assert.That(nonexistent, Is.Empty);
        }

        [Test]
        public void TestMultSync()
        {
            this.serverSyncList.Add("1");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            // add some delta and see if it applies
            this.serverSyncList.Add("2");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            Assert.That(this.clientSyncList, Is.EquivalentTo(new[] { "Hello", "World", "!", "1", "2" }));
        }

        [Test]
        public void SyncListIntest()
        {
            var serverList = new SyncList<int>();
            var clientList = new SyncList<int>();

            serverList.Add(1);
            serverList.Add(2);
            serverList.Add(3);
            SerializeHelper.SerializeDeltaTo(serverList, clientList);

            Assert.That(clientList, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void SyncListBoolTest()
        {
            var serverList = new SyncList<bool>();
            var clientList = new SyncList<bool>();

            serverList.Add(true);
            serverList.Add(false);
            serverList.Add(true);
            SerializeHelper.SerializeDeltaTo(serverList, clientList);

            Assert.That(clientList, Is.EquivalentTo(new[] { true, false, true }));
        }

        [Test]
        public void SyncListUIntTest()
        {
            var serverList = new SyncList<uint>();
            var clientList = new SyncList<uint>();

            serverList.Add(1U);
            serverList.Add(2U);
            serverList.Add(3U);
            SerializeHelper.SerializeDeltaTo(serverList, clientList);

            Assert.That(clientList, Is.EquivalentTo(new[] { 1U, 2U, 3U }));
        }

        [Test]
        public void SyncListFloatTest()
        {
            var serverList = new SyncList<float>();
            var clientList = new SyncList<float>();

            serverList.Add(1.0F);
            serverList.Add(2.0F);
            serverList.Add(3.0F);
            SerializeHelper.SerializeDeltaTo(serverList, clientList);

            Assert.That(clientList, Is.EquivalentTo(new[] { 1.0F, 2.0F, 3.0F }));
        }

        [Test]
        public void AddClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncList.OnInsert += callback;
            this.serverSyncList.Add("yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke(3, "yay");
        }

        [Test]
        public void InsertClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncList.OnInsert += callback;
            this.serverSyncList.Insert(1, "yay");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke(1, "yay");
        }

        [Test]
        public void RemoveClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.clientSyncList.OnRemove += callback;
            this.serverSyncList.Remove("World");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke(1, "World");
        }

        [Test]
        public void ClearClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncList.OnClear += callback;
            this.serverSyncList.Clear();
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke();
        }

        [Test]
        public void SetClientCallbackTest()
        {
            var callback = Substitute.For<Action<int, string, string>>();
            this.clientSyncList.OnSet += callback;
            this.serverSyncList[1] = "yo mama";
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            callback.Received().Invoke(1, "World", "yo mama");
        }

        [Test]
        public void ChangeClientCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.clientSyncList.OnChange += callback;
            this.serverSyncList.Add("1");
            this.serverSyncList.Add("2");
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);
            callback.Received(1).Invoke();
        }

        [Test]
        public void AddServerCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.serverSyncList.OnInsert += callback;
            this.serverSyncList.Add("yay");
            callback.Received().Invoke(3, "yay");
        }

        [Test]
        public void InsertServerCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.serverSyncList.OnInsert += callback;
            this.serverSyncList.Insert(1, "yay");
            callback.Received().Invoke(1, "yay");
        }

        [Test]
        public void RemoveServerCallbackTest()
        {
            var callback = Substitute.For<Action<int, string>>();
            this.serverSyncList.OnRemove += callback;
            this.serverSyncList.Remove("World");
            callback.Received().Invoke(1, "World");
        }

        [Test]
        public void ClearServerCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.serverSyncList.OnClear += callback;
            this.serverSyncList.Clear();
            callback.Received().Invoke();
        }

        [Test]
        public void SetServerCallbackTest()
        {
            var callback = Substitute.For<Action<int, string, string>>();
            this.serverSyncList.OnSet += callback;
            this.serverSyncList[1] = "yo mama";
            callback.Received().Invoke(1, "World", "yo mama");
        }

        [Test]
        public void ChangeServerCallbackTest()
        {
            var callback = Substitute.For<Action>();
            this.serverSyncList.OnChange += callback;
            this.serverSyncList.Add("1");
            this.serverSyncList.Add("2");
            // note that on the server we would receive 2 calls
            // because we are adding 2 operations separately
            // there is no way to batch operations in the server
            callback.Received().Invoke();
        }

        [Test]
        public void CountTest()
        {
            Assert.That(this.serverSyncList.Count, Is.EqualTo(3));
        }

        [Test]
        public void ReadOnlyTest()
        {
            Assert.That(this.serverSyncList.IsReadOnly, Is.False);
            Assert.That(this.clientSyncList.IsReadOnly, Is.True);
        }
        [Test]
        public void WritingToReadOnlyThrows()
        {
            Assert.Throws<InvalidOperationException>(() => { this.clientSyncList.Add("fail"); });
        }

        [Test]
        public void DirtyTest()
        {
            // Sync Delta to clear dirty
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);

            // nothing to send
            Assert.That(this.serverSyncList.IsDirty, Is.False);

            // something has changed
            this.serverSyncList.Add("1");
            Assert.That(this.serverSyncList.IsDirty, Is.True);
            SerializeHelper.SerializeDeltaTo(this.serverSyncList, this.clientSyncList);

            // data has been flushed,  should go back to clear
            Assert.That(this.serverSyncList.IsDirty, Is.False);
        }

        [Test]
        public void ObjectCanBeReusedAfterReset()
        {
            this.clientSyncList.Reset();

            // make old client the host
            var hostList = this.clientSyncList;
            var clientList2 = new SyncList<string>();

            Assert.That(hostList.IsReadOnly, Is.False);

            // Check Add and Sync without errors
            hostList.Add("hello");
            hostList.Add("world");
            SerializeHelper.SerializeDeltaTo(hostList, clientList2);
        }

        [Test]
        public void ResetShouldSetReadOnlyToFalse()
        {
            this.clientSyncList.Reset();

            Assert.That(this.clientSyncList.IsReadOnly, Is.False);
        }

        [Test]
        public void ResetShouldClearChanges()
        {
            this.serverSyncList.Reset();

            Assert.That(this.serverSyncList.ChangeCount, Is.Zero);
        }

        [Test]
        public void ResetShouldClearItems()
        {
            this.serverSyncList.Reset();

            Assert.That(this.serverSyncList, Is.Empty);
        }
    }
}

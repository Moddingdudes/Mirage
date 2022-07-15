using System.Collections.Generic;
using Mirage.Collections;
using Mirage.Serialization;
using NUnit.Framework;
using UnityEngine;

// Note: Weaver doesn't run on nested class so so use namespace to group classes instead
namespace Mirage.Tests.Runtime.Serialization.NetworkBehaviourSerialize
{
    #region No OnSerialize/OnDeserialize override
    internal abstract class AbstractBehaviour : NetworkBehaviour
    {
        public readonly SyncList<bool> syncListInAbstract = new SyncList<bool>();

        [SyncVar]
        public int SyncFieldInAbstract;
    }

    internal class BehaviourWithSyncVar : NetworkBehaviour
    {
        public readonly SyncList<bool> syncList = new SyncList<bool>();

        [SyncVar]
        public int SyncField;
    }

    internal class OverrideBehaviourFromSyncVar : AbstractBehaviour
    {

    }

    internal class OverrideBehaviourWithSyncVarFromSyncVar : AbstractBehaviour
    {
        public readonly SyncList<bool> syncListInOverride = new SyncList<bool>();

        [SyncVar]
        public int SyncFieldInOverride;
    }

    internal class MiddleClass : AbstractBehaviour
    {
        // class with no sync var
    }

    internal class SubClass : MiddleClass
    {
        // class with sync var
        // this is to make sure that override works correctly if base class doesnt have sync vars
        [SyncVar]
        public Vector3 anotherSyncField;
    }

    internal class MiddleClassWithSyncVar : AbstractBehaviour
    {
        // class with sync var
        [SyncVar]
        public string syncFieldInMiddle;
    }

    internal class SubClassFromSyncVar : MiddleClassWithSyncVar
    {
        // class with sync var
        // this is to make sure that override works correctly if base class doesnt have sync vars
        [SyncVar]
        public Vector3 syncFieldInSub;
    }
    #endregion

    #region OnSerialize/OnDeserialize override


    internal class BehaviourWithSyncVarWithOnSerialize : NetworkBehaviour
    {
        public readonly SyncList<bool> syncList = new SyncList<bool>();

        [SyncVar]
        public int SyncField;

        public float customSerializeField;

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            writer.WriteSingle(this.customSerializeField);
            return base.OnSerialize(writer, initialState);
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            this.customSerializeField = reader.ReadSingle();
            base.OnDeserialize(reader, initialState);
        }
    }

    internal class OverrideBehaviourFromSyncVarWithOnSerialize : AbstractBehaviour
    {
        public float customSerializeField;

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            writer.WriteSingle(this.customSerializeField);
            return base.OnSerialize(writer, initialState);
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            this.customSerializeField = reader.ReadSingle();
            base.OnDeserialize(reader, initialState);
        }
    }

    internal class OverrideBehaviourWithSyncVarFromSyncVarWithOnSerialize : AbstractBehaviour
    {
        public readonly SyncList<bool> syncListInOverride = new SyncList<bool>();

        [SyncVar]
        public int SyncFieldInOverride;

        public float customSerializeField;

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            writer.WriteSingle(this.customSerializeField);
            return base.OnSerialize(writer, initialState);
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            this.customSerializeField = reader.ReadSingle();
            base.OnDeserialize(reader, initialState);
        }
    }
    #endregion

    public class NetworkBehaviourSerializeTest
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        [TearDown]
        public void TearDown()
        {
            // Clean up all created objects
            foreach (var item in this.createdObjects)
            {
                Object.DestroyImmediate(item);
            }
            this.createdObjects.Clear();
        }

        private T CreateBehaviour<T>() where T : NetworkBehaviour
        {
            var go1 = new GameObject();
            go1.AddComponent<NetworkIdentity>();
            this.createdObjects.Add(go1);
            return go1.AddComponent<T>();
        }

        private static void SyncNetworkBehaviour(NetworkBehaviour source, NetworkBehaviour target, bool initialState)
        {
            using (var writer = NetworkWriterPool.GetWriter())
            {
                source.OnSerialize(writer, initialState);

                using (var reader = NetworkReaderPool.GetReader(writer.ToArraySegment(), target.World))
                {
                    target.OnDeserialize(reader, initialState);
                }
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void BehaviourWithSyncVarTest(bool initialState)
        {
            var source = this.CreateBehaviour<BehaviourWithSyncVar>();
            var target = this.CreateBehaviour<BehaviourWithSyncVar>();

            source.SyncField = 10;
            source.syncList.Add(true);

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncField, Is.EqualTo(10));
            Assert.That(target.syncList.Count, Is.EqualTo(1));
            Assert.That(target.syncList[0], Is.True);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void OverrideBehaviourFromSyncVarTest(bool initialState)
        {
            var source = this.CreateBehaviour<OverrideBehaviourFromSyncVar>();
            var target = this.CreateBehaviour<OverrideBehaviourFromSyncVar>();

            source.SyncFieldInAbstract = 12;
            source.syncListInAbstract.Add(true);
            source.syncListInAbstract.Add(false);

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncFieldInAbstract, Is.EqualTo(12));
            Assert.That(target.syncListInAbstract.Count, Is.EqualTo(2));
            Assert.That(target.syncListInAbstract[0], Is.True);
            Assert.That(target.syncListInAbstract[1], Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void OverrideBehaviourWithSyncVarFromSyncVarTest(bool initialState)
        {
            var source = this.CreateBehaviour<OverrideBehaviourWithSyncVarFromSyncVar>();
            var target = this.CreateBehaviour<OverrideBehaviourWithSyncVarFromSyncVar>();

            source.SyncFieldInAbstract = 10;
            source.syncListInAbstract.Add(true);

            source.SyncFieldInOverride = 52;
            source.syncListInOverride.Add(false);
            source.syncListInOverride.Add(true);

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncFieldInAbstract, Is.EqualTo(10));
            Assert.That(target.syncListInAbstract.Count, Is.EqualTo(1));
            Assert.That(target.syncListInAbstract[0], Is.True);


            Assert.That(target.SyncFieldInOverride, Is.EqualTo(52));
            Assert.That(target.syncListInOverride.Count, Is.EqualTo(2));
            Assert.That(target.syncListInOverride[0], Is.False);
            Assert.That(target.syncListInOverride[1], Is.True);
        }


        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SubClassTest(bool initialState)
        {
            var source = this.CreateBehaviour<SubClass>();
            var target = this.CreateBehaviour<SubClass>();

            source.SyncFieldInAbstract = 10;
            source.syncListInAbstract.Add(true);

            source.anotherSyncField = new Vector3(40, 20, 10);

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncFieldInAbstract, Is.EqualTo(10));
            Assert.That(target.syncListInAbstract.Count, Is.EqualTo(1));
            Assert.That(target.syncListInAbstract[0], Is.True);

            Assert.That(target.anotherSyncField, Is.EqualTo(new Vector3(40, 20, 10)));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SubClassFromSyncVarTest(bool initialState)
        {
            var source = this.CreateBehaviour<SubClassFromSyncVar>();
            var target = this.CreateBehaviour<SubClassFromSyncVar>();

            source.SyncFieldInAbstract = 10;
            source.syncListInAbstract.Add(true);

            source.syncFieldInMiddle = "Hello World!";
            source.syncFieldInSub = new Vector3(40, 20, 10);

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncFieldInAbstract, Is.EqualTo(10));
            Assert.That(target.syncListInAbstract.Count, Is.EqualTo(1));
            Assert.That(target.syncListInAbstract[0], Is.True);

            Assert.That(target.syncFieldInMiddle, Is.EqualTo("Hello World!"));
            Assert.That(target.syncFieldInSub, Is.EqualTo(new Vector3(40, 20, 10)));
        }



        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void BehaviourWithSyncVarWithOnSerializeTest(bool initialState)
        {
            var source = this.CreateBehaviour<BehaviourWithSyncVarWithOnSerialize>();
            var target = this.CreateBehaviour<BehaviourWithSyncVarWithOnSerialize>();

            source.SyncField = 10;
            source.syncList.Add(true);

            source.customSerializeField = 20.5f;

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncField, Is.EqualTo(10));
            Assert.That(target.syncList.Count, Is.EqualTo(1));
            Assert.That(target.syncList[0], Is.True);

            Assert.That(target.customSerializeField, Is.EqualTo(20.5f));
        }


        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void OverrideBehaviourFromSyncVarWithOnSerializeTest(bool initialState)
        {
            var source = this.CreateBehaviour<OverrideBehaviourFromSyncVarWithOnSerialize>();
            var target = this.CreateBehaviour<OverrideBehaviourFromSyncVarWithOnSerialize>();

            source.SyncFieldInAbstract = 12;
            source.syncListInAbstract.Add(true);
            source.syncListInAbstract.Add(false);

            source.customSerializeField = 20.5f;

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncFieldInAbstract, Is.EqualTo(12));
            Assert.That(target.syncListInAbstract.Count, Is.EqualTo(2));
            Assert.That(target.syncListInAbstract[0], Is.True);
            Assert.That(target.syncListInAbstract[1], Is.False);

            Assert.That(target.customSerializeField, Is.EqualTo(20.5f));
        }


        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void OverrideBehaviourWithSyncVarFromSyncVarWithOnSerializeTest(bool initialState)
        {
            var source = this.CreateBehaviour<OverrideBehaviourWithSyncVarFromSyncVarWithOnSerialize>();
            var target = this.CreateBehaviour<OverrideBehaviourWithSyncVarFromSyncVarWithOnSerialize>();

            source.SyncFieldInAbstract = 10;
            source.syncListInAbstract.Add(true);

            source.SyncFieldInOverride = 52;
            source.syncListInOverride.Add(false);
            source.syncListInOverride.Add(true);

            source.customSerializeField = 20.5f;

            SyncNetworkBehaviour(source, target, initialState);

            Assert.That(target.SyncFieldInAbstract, Is.EqualTo(10));
            Assert.That(target.syncListInAbstract.Count, Is.EqualTo(1));
            Assert.That(target.syncListInAbstract[0], Is.True);


            Assert.That(target.SyncFieldInOverride, Is.EqualTo(52));
            Assert.That(target.syncListInOverride.Count, Is.EqualTo(2));
            Assert.That(target.syncListInOverride[0], Is.False);
            Assert.That(target.syncListInOverride[1], Is.True);

            Assert.That(target.customSerializeField, Is.EqualTo(20.5f));
        }
    }
}

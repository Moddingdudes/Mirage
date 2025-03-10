using System.Collections;
using Mirage.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime.Syncing
{
    public class MockPlayerWithList : MockPlayer
    {
        public SyncList<int> MySyncList = new SyncList<int>();
    }

    // different Directions to test

    // server -> owner
    // server -> observer
    // server -> owner,observer

    // owner -> server
    // owner -> server,observer

    // owner,server -> owner,server
    // owner,server -> server,observer
    // owner,server -> owner,server,observer

    public class SyncDirectionObjectFromServer : SyncDirectionTestBase<MockPlayerWithList>
    {
        private const int listValue = 5;

        [UnityTest]
        public IEnumerator ToOwner()
        {
            SetDirection(SyncFrom.Server, SyncTo.Owner);

            ServerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;
            ServerComponent.MySyncList.Add(listValue);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(OwnerComponent.target.NetId, Is.EqualTo(ServerExtraIdentity.NetId));
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue));

            Assert.That(ObserverComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ToObserver()
        {
            SetDirection(SyncFrom.Server, SyncTo.ObserversOnly);

            ServerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;
            ServerComponent.MySyncList.Add(listValue);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(OwnerComponent.target, Is.Null);
            Assert.That(OwnerComponent.MySyncList.Count, Is.Zero);

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ObserverComponent.target, Is.EqualTo(_remoteClients[1].Get(ServerExtraIdentity)));
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue));
        }

        [UnityTest]
        public IEnumerator ToOwnerAndObserver()
        {
            SetDirection(SyncFrom.Server, SyncTo.OwnerAndObservers);

            ServerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;
            ServerComponent.MySyncList.Add(listValue);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(OwnerComponent.target.NetId, Is.EqualTo(ServerExtraIdentity.NetId));
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue));

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ObserverComponent.target, Is.EqualTo(_remoteClients[1].Get(ServerExtraIdentity)));
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue));
        }
    }


    public class SyncDirectionObjectFromOwner : SyncDirectionTestBase<MockPlayerWithList>
    {
        private const int listValue = 5;

        [UnityTest]
        public IEnumerator ToServer()
        {
            SetDirection(SyncFrom.Owner, SyncTo.Server);

            OwnerComponent.guild = guild;
            OwnerComponent.target = _remoteClients[0].Get(ServerExtraIdentity);
            OwnerComponent.MySyncList.Add(listValue);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue));

            Assert.That(ObserverComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(ObserverComponent.target, Is.Null);
            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ToServerAndObservers()
        {
            SetDirection(SyncFrom.Owner, SyncTo.Server | SyncTo.ObserversOnly);

            OwnerComponent.guild = guild;
            OwnerComponent.target = _remoteClients[0].Get(ServerExtraIdentity);
            OwnerComponent.MySyncList.Add(listValue);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue));

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ObserverComponent.target, Is.EqualTo(_remoteClients[1].Get(ServerExtraIdentity)));
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue));
        }
    }

    public class SyncDirectionObjectFromServerAndOwner : SyncDirectionTestBase<MockPlayerWithList>
    {
        private const int listValue1 = 5;
        private const int listValue2 = 10;

        [UnityTest]
        public IEnumerator ToServerAndOwner()
        {
            SetDirection(SyncFrom.Server | SyncFrom.Owner, SyncTo.Server | SyncTo.Owner);

            ServerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;
            ServerComponent.MySyncList.Add(listValue1);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(OwnerComponent.target.NetId, Is.EqualTo(ServerExtraIdentity.NetId));
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue1));

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));

            Assert.That(ObserverComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(ObserverComponent.target, Is.Null);
            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);

            OwnerComponent.guild = guild2;
            OwnerComponent.target = null;
            OwnerComponent.MySyncList.Add(listValue2);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(OwnerComponent.target, Is.Null);
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(OwnerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(ServerComponent.target, Is.Null);
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ServerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ObserverComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(ObserverComponent.target, Is.Null);
            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ToServerAndObservers()
        {
            SetDirection(SyncFrom.Server | SyncFrom.Owner, SyncTo.Server | SyncTo.ObserversOnly);

            ServerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;
            ServerComponent.MySyncList.Add(listValue1);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(OwnerComponent.target, Is.Null);
            Assert.That(OwnerComponent.MySyncList.Count, Is.Zero);

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ObserverComponent.target, Is.EqualTo(_remoteClients[1].Get(ServerExtraIdentity)));
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue1));

            // just update guild
            OwnerComponent.guild = guild2;
            OwnerComponent.MySyncList.Add(listValue2);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(OwnerComponent.target, Is.Null);
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue2));

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild2.name));
            // target should not be changed
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ServerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(ObserverComponent.target, Is.EqualTo(_remoteClients[1].Get(ServerExtraIdentity)));
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ObserverComponent.MySyncList[1], Is.EqualTo(listValue2));
        }

        [UnityTest]
        public IEnumerator ToServerOwnerAndObservers()
        {
            SetDirection(SyncFrom.Server | SyncFrom.Owner, SyncTo.Server | SyncTo.Owner | SyncTo.ObserversOnly);

            ServerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;
            ServerComponent.MySyncList.Add(listValue1);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(OwnerComponent.target.NetId, Is.EqualTo(ServerExtraIdentity.NetId));
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue1));

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ObserverComponent.target, Is.EqualTo(_remoteClients[1].Get(ServerExtraIdentity)));
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(1));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue1));

            OwnerComponent.guild = guild2;
            OwnerComponent.target = null;
            OwnerComponent.MySyncList.Add(listValue2);


            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(OwnerComponent.target, Is.Null);
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(OwnerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(ServerComponent.target, Is.Null);
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ServerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(ObserverComponent.target, Is.Null);
            Assert.That(ObserverComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ObserverComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ObserverComponent.MySyncList[1], Is.EqualTo(listValue2));

            OwnerComponent.MySyncList.Clear();

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(OwnerComponent.target, Is.Null);
            Assert.That(OwnerComponent.MySyncList.Count, Is.Zero);

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(ServerComponent.target, Is.Null);
            Assert.That(ServerComponent.MySyncList.Count, Is.Zero);

            Assert.That(ObserverComponent.guild.name, Is.EqualTo(guild2.name));
            Assert.That(ObserverComponent.target, Is.Null);
            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CanSetDifferentVarsOnDifferentFrom()
        {
            SetDirection(SyncFrom.Server | SyncFrom.Owner, SyncTo.Server | SyncTo.Owner);

            OwnerComponent.guild = guild;
            ServerComponent.target = ServerExtraIdentity;

            ServerComponent.MySyncList.Add(listValue1);
            OwnerComponent.MySyncList.Add(listValue2);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(OwnerComponent.target.NetId, Is.EqualTo(ServerExtraIdentity.NetId));
            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(2));
            // owner added 2 first, then received 1 from server
            // this could lead to desync, but that is a risk of using by sync directions 
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue2));
            Assert.That(OwnerComponent.MySyncList[1], Is.EqualTo(listValue1));

            Assert.That(ServerComponent.guild.name, Is.EqualTo(guild.name));
            Assert.That(ServerComponent.target, Is.EqualTo(ServerExtraIdentity));
            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ServerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ObserverComponent.guild.name, Is.Null.Or.Empty);
            Assert.That(ObserverComponent.target, Is.Null);
            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CanSetDifferentVarsOnDifferentFrom_onlySyncObject()
        {
            SetDirection(SyncFrom.Server | SyncFrom.Owner, SyncTo.Server | SyncTo.Owner);

            ServerComponent.MySyncList.Add(listValue1);
            OwnerComponent.MySyncList.Add(listValue2);

            // wait for sync
            yield return null;
            yield return null;

            Assert.That(OwnerComponent.MySyncList.Count, Is.EqualTo(2));
            // owner added 2 first, then received 1 from server
            // this could lead to desync, but that is a risk of using by sync directions
            Assert.That(OwnerComponent.MySyncList[0], Is.EqualTo(listValue2));
            Assert.That(OwnerComponent.MySyncList[1], Is.EqualTo(listValue1));

            Assert.That(ServerComponent.MySyncList.Count, Is.EqualTo(2));
            Assert.That(ServerComponent.MySyncList[0], Is.EqualTo(listValue1));
            Assert.That(ServerComponent.MySyncList[1], Is.EqualTo(listValue2));

            Assert.That(ObserverComponent.MySyncList.Count, Is.Zero);
        }
    }
}

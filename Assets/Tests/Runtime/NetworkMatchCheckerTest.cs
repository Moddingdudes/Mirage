using System;
using System.Collections.Generic;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mirage.Tests.Runtime
{
    public class NetworkMatchCheckerTest
    {
        private GameObject serverGO;
        private NetworkServer server;
        private ServerObjectManager serverObjectManager;
        private GameObject character1;
        private GameObject character2;
        private GameObject character3;
        private NetworkMatchChecker player1MatchChecker;
        private NetworkMatchChecker player2MatchChecker;
        private NetworkPlayer player1Connection;
        private NetworkPlayer player2Connection;
        private NetworkPlayer player3Connection;
        private Dictionary<Guid, HashSet<NetworkIdentity>> matchPlayers;

        [SetUp]
        public void Setup()
        {
            // todo use Substitute for interfaces instead of gameobjeccts for this test

            this.serverGO = new GameObject("Network Server", typeof(TestSocketFactory), typeof(NetworkServer), typeof(ServerObjectManager));

            this.server = this.serverGO.GetComponent<NetworkServer>();
            this.serverObjectManager = this.serverGO.GetComponent<ServerObjectManager>();
            this.serverObjectManager.Server = this.server;

            this.character1 = new GameObject("TestCharacter1", typeof(NetworkIdentity), typeof(NetworkMatchChecker));
            this.character2 = new GameObject("TestCharacter2", typeof(NetworkIdentity), typeof(NetworkMatchChecker));
            this.character3 = new GameObject("TestCharacter3", typeof(NetworkIdentity));


            this.character1.GetComponent<NetworkIdentity>().Server = this.server;
            this.character1.GetComponent<NetworkIdentity>().ServerObjectManager = this.serverObjectManager;
            this.character2.GetComponent<NetworkIdentity>().Server = this.server;
            this.character2.GetComponent<NetworkIdentity>().ServerObjectManager = this.serverObjectManager;
            this.character3.GetComponent<NetworkIdentity>().Server = this.server;
            this.character3.GetComponent<NetworkIdentity>().ServerObjectManager = this.serverObjectManager;

            this.player1MatchChecker = this.character1.GetComponent<NetworkMatchChecker>();
            this.player2MatchChecker = this.character2.GetComponent<NetworkMatchChecker>();


            this.player1Connection = CreatePlayer(this.character1);
            this.player2Connection = CreatePlayer(this.character2);
            this.player3Connection = CreatePlayer(this.character3);
            var g = GetMatchPlayersDictionary();
            this.matchPlayers = g;
        }

        private static Dictionary<Guid, HashSet<NetworkIdentity>> GetMatchPlayersDictionary()
        {
            var type = typeof(NetworkMatchChecker);
            var fieldInfo = type.GetField("matchPlayers", BindingFlags.Static | BindingFlags.NonPublic);
            return (Dictionary<Guid, HashSet<NetworkIdentity>>)fieldInfo.GetValue(null);
        }

        private static NetworkPlayer CreatePlayer(GameObject character)
        {
            var player = new NetworkPlayer(Substitute.For<SocketLayer.IConnection>())
            {
                Identity = character.GetComponent<NetworkIdentity>()
            };
            player.Identity.Owner = player;
            player.SceneIsReady = true;
            return player;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(this.character1);
            Object.DestroyImmediate(this.character2);
            Object.DestroyImmediate(this.character3);

            Object.DestroyImmediate(this.serverGO);
            this.matchPlayers.Clear();
            this.matchPlayers = null;
        }

        private static void SetMatchId(NetworkMatchChecker target, Guid guid)
        {
            // set using reflection so bypass property
            var field = typeof(NetworkMatchChecker).GetField("currentMatch", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, guid);
        }

        [Test]
        public void OnCheckObserverShouldBeTrueForSameMatchId()
        {
            var guid = Guid.NewGuid().ToString();

            SetMatchId(this.player1MatchChecker, new Guid(guid));
            SetMatchId(this.player2MatchChecker, new Guid(guid));

            var player1Visable = this.player1MatchChecker.OnCheckObserver(this.player1Connection);
            Assert.IsTrue(player1Visable);

            var player2Visable = this.player1MatchChecker.OnCheckObserver(this.player2Connection);
            Assert.IsTrue(player2Visable);
        }

        [Test]
        public void OnCheckObserverShouldBeFalseForDifferentMatchId()
        {
            var guid1 = Guid.NewGuid().ToString();
            var guid2 = Guid.NewGuid().ToString();

            SetMatchId(this.player1MatchChecker, new Guid(guid1));
            SetMatchId(this.player2MatchChecker, new Guid(guid2));

            var player1VisableToPlayer1 = this.player1MatchChecker.OnCheckObserver(this.player1Connection);
            Assert.IsTrue(player1VisableToPlayer1);

            var player2VisableToPlayer1 = this.player1MatchChecker.OnCheckObserver(this.player2Connection);
            Assert.IsFalse(player2VisableToPlayer1);


            var player1VisableToPlayer2 = this.player2MatchChecker.OnCheckObserver(this.player1Connection);
            Assert.IsFalse(player1VisableToPlayer2);

            var player2VisableToPlayer2 = this.player2MatchChecker.OnCheckObserver(this.player2Connection);
            Assert.IsTrue(player2VisableToPlayer2);
        }

        [Test]
        public void OnCheckObserverShouldBeFalseIfObjectDoesNotHaveNetworkMatchChecker()
        {
            var guid = Guid.NewGuid().ToString();

            SetMatchId(this.player1MatchChecker, new Guid(guid));

            var player3Visable = this.player1MatchChecker.OnCheckObserver(this.player3Connection);
            Assert.IsFalse(player3Visable);
        }

        [Test]
        public void OnCheckObserverShouldBeFalseForEmptyGuid()
        {
            var guid = Guid.Empty.ToString();

            SetMatchId(this.player1MatchChecker, new Guid(guid));
            SetMatchId(this.player2MatchChecker, new Guid(guid));

            var player1Visable = this.player1MatchChecker.OnCheckObserver(this.player1Connection);
            Assert.IsFalse(player1Visable);

            var player2Visable = this.player1MatchChecker.OnCheckObserver(this.player2Connection);
            Assert.IsFalse(player2Visable);
        }

        [Test]
        public void SettingMatchIdShouldRebuildObservers()
        {
            var guidMatch1 = Guid.NewGuid().ToString();

            // make players join same match
            this.player1MatchChecker.MatchId = new Guid(guidMatch1);
            this.player2MatchChecker.MatchId = new Guid(guidMatch1);

            // check player1's observers contains player 2
            Assert.That(this.player1MatchChecker.Identity.observers, Contains.Item(this.player2MatchChecker.Owner));
            // check player2's observers contains player 1
            Assert.That(this.player2MatchChecker.Identity.observers, Contains.Item(this.player1MatchChecker.Owner));
        }

        [Test]
        public void ChangingMatchIdShouldRebuildObservers()
        {
            var guidMatch1 = Guid.NewGuid().ToString();
            var guidMatch2 = Guid.NewGuid().ToString();

            // make players join same match
            this.player1MatchChecker.MatchId = new Guid(guidMatch1);
            this.player2MatchChecker.MatchId = new Guid(guidMatch1);

            // make player2 join different match
            this.player2MatchChecker.MatchId = new Guid(guidMatch2);

            // check player1's observers does NOT contain player 2
            Assert.That(this.player1MatchChecker.Identity.observers, !Contains.Item(this.player2MatchChecker.Owner));
            // check player2's observers does NOT contain player 1
            Assert.That(this.player2MatchChecker.Identity.observers, !Contains.Item(this.player1MatchChecker.Owner));
        }

        [Test]
        public void ClearingMatchIdShouldRebuildObservers()
        {
            var guidMatch1 = Guid.NewGuid().ToString();

            // make players join same match
            this.player1MatchChecker.MatchId = new Guid(guidMatch1);
            this.player2MatchChecker.MatchId = new Guid(guidMatch1);

            // make player 2 leave match
            this.player2MatchChecker.MatchId = Guid.Empty;

            // check player1's observers does NOT contain player 2
            Assert.That(this.player1MatchChecker.Identity.observers, !Contains.Item(this.player2MatchChecker.Owner));
            // check player2's observers does NOT contain player 1
            Assert.That(this.player2MatchChecker.Identity.observers, !Contains.Item(this.player1MatchChecker.Owner));
        }
    }
}

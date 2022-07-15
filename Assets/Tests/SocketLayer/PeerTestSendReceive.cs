using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirage.Tests;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirage.SocketLayer.Tests.PeerTests
{
    public static class PeerTestExtensions
    {
        public static void UpdateTest(this Peer peer)
        {
            peer.UpdateReceive();
            peer.UpdateSent();
        }
    }
    [Category("SocketLayer"), Description("integration test to make sure that send and receiving works as a whole")]
    public class PeerTestSendReceive
    {
        private const int ClientCount = 4;
        private PeerInstanceWithSocket server;
        private PeerInstanceWithSocket[] clients;
        private List<IConnection> clientConnections;
        private List<IConnection> serverConnections;
        private int maxFragmentMessageSize;
        private float NotifyWaitTime;

        [SetUp]
        public void SetUp()
        {
            this.clientConnections = new List<IConnection>();
            this.serverConnections = new List<IConnection>();

            var config = new Config { MaxConnections = ClientCount };
            this.maxFragmentMessageSize = config.MaxReliableFragments * (PeerTestBase.MAX_PACKET_SIZE - AckSystem.MIN_RELIABLE_FRAGMENT_HEADER_SIZE);
            this.NotifyWaitTime = config.TimeBeforeEmptyAck * 2;


            this.server = new PeerInstanceWithSocket(config);
            this.clients = new PeerInstanceWithSocket[ClientCount];
            Action<IConnection> serverConnect = (conn) => this.serverConnections.Add(conn);
            this.server.peer.OnConnected += serverConnect;

            this.server.peer.Bind(TestEndPoint.CreateSubstitute());
            for (var i = 0; i < ClientCount; i++)
            {
                this.clients[i] = new PeerInstanceWithSocket(config);
                this.clientConnections.Add(this.clients[i].peer.Connect(this.server.endPoint));
            }

            this.UpdateAll();
        }

        private void UpdateAll()
        {
            this.server.peer.UpdateTest();
            for (var i = 0; i < ClientCount; i++)
            {
                this.clients[i].peer.UpdateTest();
            }
        }

        private void CheckClientReceived(byte[] message)
        {
            this.UpdateAll();
            this.UpdateAll();

            // check each client got packet once
            for (var i = 0; i < ClientCount; i++)
            {
                var handler = this.clients[i].dataHandler;

                //handler.Received(1).ReceiveMessage(clientConnections[i], Arg.Is<ArraySegment<byte>>(x => x.SequenceEqual(message)));
                handler.Received(1).ReceiveMessage(this.clientConnections[i], Arg.Is<ArraySegment<byte>>(x => this.DebugSequenceEqual(message, x)));
            }
        }

        private void CheckServerReceived(byte[] message)
        {
            this.UpdateAll();
            this.UpdateAll();

            var handler = this.server.dataHandler;
            // check each client sent packet once
            for (var i = 0; i < ClientCount; i++)
            {
                //handler.Received(1).ReceiveMessage(serverConnections[i], Arg.Is<ArraySegment<byte>>(x => x.SequenceEqual(message)));
                handler.Received(1).ReceiveMessage(this.serverConnections[i], Arg.Is<ArraySegment<byte>>(x => this.DebugSequenceEqual(message, x)));
            }
        }

        [Test]
        public void SeverUnreliableSend()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            // send 1 message to each client
            for (var i = 0; i < ClientCount; i++)
            {
                this.serverConnections[i].SendUnreliable(message);
            }

            this.CheckClientReceived(message);
        }


        [Test]
        public void ClientUnreliableSend()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            // send 1 message from each client
            for (var i = 0; i < ClientCount; i++)
            {
                this.clientConnections[i].SendUnreliable(message);
            }

            this.CheckServerReceived(message);
        }

        [Test]
        public void ServerNotifySend()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            // send 1 message to each client
            for (var i = 0; i < ClientCount; i++)
            {
                this.serverConnections[i].SendNotify(message);
            }

            this.CheckClientReceived(message);
        }

        [Test]
        public void ClientNotifySend()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            // send 1 message from each client
            for (var i = 0; i < ClientCount; i++)
            {
                this.clientConnections[i].SendNotify(message);
            }

            this.CheckServerReceived(message);
        }

        [UnityTest]
        public IEnumerator ServerNotifySendMarkedAsReceived()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            var received = new Action[ClientCount];
            var lost = new Action[ClientCount];
            // send 1 message to each client
            for (var i = 0; i < ClientCount; i++)
            {
                var token = this.serverConnections[i].SendNotify(message);

                received[i] = Substitute.For<Action>();
                lost[i] = Substitute.For<Action>();
                token.Delivered += received[i];
                token.Lost += lost[i];
            }

            var end = UnityEngine.Time.time + this.NotifyWaitTime;
            while (end > UnityEngine.Time.time)
            {
                this.UpdateAll();
                yield return null;
            }

            for (var i = 0; i < ClientCount; i++)
            {
                received[i].Received(1).Invoke();
                lost[i].DidNotReceive().Invoke();
            }
        }

        [UnityTest]
        public IEnumerator ClientNotifySendMarkedAsReceived()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            var received = new Action[ClientCount];
            var lost = new Action[ClientCount];
            // send 1 message from each client
            for (var i = 0; i < ClientCount; i++)
            {
                var token = this.clientConnections[i].SendNotify(message);

                received[i] = Substitute.For<Action>();
                lost[i] = Substitute.For<Action>();
                token.Delivered += received[i];
                token.Lost += lost[i];
            }

            var end = UnityEngine.Time.time + this.NotifyWaitTime;
            while (end > UnityEngine.Time.time)
            {
                this.UpdateAll();
                yield return null;
            }

            for (var i = 0; i < ClientCount; i++)
            {
                received[i].Received(1).Invoke();
                lost[i].DidNotReceive().Invoke();
            }
        }

        [UnityTest]
        public IEnumerator ServerNotifySendCallbacksMarkedAsReceived()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            var callBacks = new INotifyCallBack[ClientCount];
            // send 1 message to each client
            for (var i = 0; i < ClientCount; i++)
            {
                callBacks[i] = Substitute.For<INotifyCallBack>();
                this.serverConnections[i].SendNotify(message, callBacks[i]);
            }

            var end = UnityEngine.Time.time + this.NotifyWaitTime;
            while (end > UnityEngine.Time.time)
            {
                this.UpdateAll();
                yield return null;
            }

            for (var i = 0; i < ClientCount; i++)
            {
                callBacks[i].Received(1).OnDelivered();
                callBacks[i].DidNotReceive().OnLost();
            }
        }

        [UnityTest]
        public IEnumerator ClientNotifySendCallbacksMarkedAsReceived()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            var callBacks = new INotifyCallBack[ClientCount];
            // send 1 message from each client
            for (var i = 0; i < ClientCount; i++)
            {
                callBacks[i] = Substitute.For<INotifyCallBack>();
                this.clientConnections[i].SendNotify(message, callBacks[i]);
            }

            var end = UnityEngine.Time.time + this.NotifyWaitTime;
            while (end > UnityEngine.Time.time)
            {
                this.UpdateAll();
                yield return null;
            }

            for (var i = 0; i < ClientCount; i++)
            {
                callBacks[i].Received(1).OnDelivered();
                callBacks[i].DidNotReceive().OnLost();
            }
        }

        [Test]
        public void ServerReliableSend()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            // send 1 message to each client
            for (var i = 0; i < ClientCount; i++)
            {
                this.serverConnections[i].SendReliable(message);
            }

            this.CheckClientReceived(message);
        }

        [Test]
        public void ClientReliableSend()
        {
            var message = Enumerable.Range(10, 20).Select(x => (byte)x).ToArray();

            // send 1 message from each client
            for (var i = 0; i < ClientCount; i++)
            {
                this.clientConnections[i].SendReliable(message);
            }

            this.CheckServerReceived(message);
        }

        [Test]
        [TestCase(1, 5)]
        [TestCase(0.8f, 4)]
        [TestCase(0.5f, 3)]
        [TestCase(0.3f, 2)]
        [TestCase(0.2f, 1)]
        public void FragmentedSend(float maxMultiplier, int expectedFragments)
        {
            var size = (int)(this.maxFragmentMessageSize * maxMultiplier);
            var message = Enumerable.Range(10, size).Select(x => (byte)x).ToArray();

            var sentCount = this.server.socket.Sent.Count;

            this.serverConnections[0].SendReliable(message);
            var handler = this.clients[0].dataHandler;

            // change in sent
            Assert.That(this.server.socket.Sent.Count - sentCount, Is.EqualTo(expectedFragments));

            this.UpdateAll();

            handler.Received(1).ReceiveMessage(this.clientConnections[0], Arg.Is<ArraySegment<byte>>(x => x.SequenceEqual(message)));
        }

        [Test]
        public void FragmentedSendThrowsIfTooBig()
        {
            var message = Enumerable.Range(10, this.maxFragmentMessageSize + 1).Select(x => (byte)x).ToArray();

            Assert.Throws<ArgumentException>(() =>
            {
                this.serverConnections[0].SendReliable(message);
            });

            this.UpdateAll();

            var handler = this.clients[0].dataHandler;
            handler.DidNotReceive().ReceiveMessage(Arg.Any<IConnection>(), Arg.Any<ArraySegment<byte>>());
        }



        private bool DebugSequenceEqual(byte[] inMsg, ArraySegment<byte> outMsg)
        {
            if (inMsg.Length != outMsg.Count) { return false; }

            for (var i = 0; i < inMsg.Length; i++)
            {
                if (inMsg[i] != outMsg.Array[outMsg.Offset + i]) { return false; }
            }

            return true;
        }
    }
}

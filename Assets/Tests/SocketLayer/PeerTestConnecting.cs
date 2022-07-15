using System;
using System.Collections.Generic;
using System.Linq;
using Mirage.Tests;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.SocketLayer.Tests.PeerTests
{
    [Category("SocketLayer"), Description("tests using multiple instances of peer to check they can connect to each other")]
    public class PeerTestConnecting
    {
        private const int ClientCount = 4;
        private PeerInstanceWithSocket server;
        private PeerInstanceWithSocket[] clients;

        [SetUp]
        public void SetUp()
        {
            this.server = new PeerInstanceWithSocket(new Config { MaxConnections = ClientCount });
            this.clients = new PeerInstanceWithSocket[ClientCount];
            for (var i = 0; i < ClientCount; i++)
            {
                this.clients[i] = new PeerInstanceWithSocket();
            }
        }

        [Test]
        public void ServerAcceptsAllClients()
        {
            this.server.peer.Bind(TestEndPoint.CreateSubstitute());

            var connectAction = Substitute.For<Action<IConnection>>();
            this.server.peer.OnConnected += connectAction;

            for (var i = 0; i < ClientCount; i++)
            {
                // tell client i to connect
                this.clients[i].peer.Connect(this.server.endPoint);
                var clientConnectAction = Substitute.For<Action<IConnection>>();
                this.clients[i].peer.OnConnected += clientConnectAction;

                // no change untill update
                Assert.That(this.server.socket.Sent.Count, Is.EqualTo(i));
                connectAction.ReceivedWithAnyArgs(i).Invoke(default);

                // run tick on server, should read packet from client i
                this.server.peer.UpdateTest();

                // server invokes connect event 
                connectAction.ReceivedWithAnyArgs(i + 1).Invoke(default);

                // sever send accept packet
                Assert.That(this.server.socket.Sent.Count, Is.EqualTo(i + 1));
                var lastSent = this.server.socket.Sent.Last();
                Assert.That(lastSent.endPoint, Is.EqualTo(this.clients[i].socket.endPoint));
                // check first 2 bytes of message
                Assert.That(ArgCollection.AreEquivalentIgnoringLength(lastSent.data, new byte[2] {
                    (byte)PacketType.Command,
                    (byte)Commands.ConnectionAccepted
                }));

                // no change on cleint till update
                clientConnectAction.ReceivedWithAnyArgs(0).Invoke(default);
                this.clients[i].peer.UpdateTest();
                clientConnectAction.ReceivedWithAnyArgs(1).Invoke(default);
            }
        }

        [Test]
        public void EachServerConnectionIsANewInstance()
        {
            this.server.peer.Bind(TestEndPoint.CreateSubstitute());
            var serverConnections = new List<IConnection>();

            Action<IConnection> connectAction = (conn) =>
            {
                serverConnections.Add(conn);
            };
            this.server.peer.OnConnected += connectAction;

            for (var i = 0; i < ClientCount; i++)
            {
                // tell client i to connect
                this.clients[i].peer.Connect(this.server.endPoint);

                // run tick on server, should read packet from client i
                this.server.peer.UpdateTest();

                Assert.That(serverConnections, Is.Unique);
            }
        }
    }
}

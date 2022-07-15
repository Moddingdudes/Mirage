using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mirage.Tests.Runtime.Host
{
    [TestFixture]
    public class NetworkServerTest : HostSetup<MockComponent>
    {
        private readonly List<INetworkPlayer> serverConnectedCalls = new List<INetworkPlayer>();
        private readonly List<INetworkPlayer> clientConnectedCalls = new List<INetworkPlayer>();

        public override void ExtraSetup()
        {
            this.serverConnectedCalls.Clear();
            this.clientConnectedCalls.Clear();

            this.server.Connected.AddListener(player => this.serverConnectedCalls.Add(player));
            this.client.Connected.AddListener(player => this.clientConnectedCalls.Add(player));
        }

        [Test]
        public void ConnectedEventIsCalledOnceForServer()
        {
            Assert.That(this.serverConnectedCalls, Has.Count.EqualTo(1));
            Assert.That(this.serverConnectedCalls[0].Connection, Is.TypeOf<PipePeerConnection>());
        }
        [Test]
        public void ConnectedEventIsCalledOnceForClient()
        {
            Assert.That(this.clientConnectedCalls, Has.Count.EqualTo(1));
            Assert.That(this.clientConnectedCalls[0].Connection, Is.TypeOf<PipePeerConnection>());
        }


        [Test]
        public void LocalClientActiveTest()
        {
            Assert.That(this.server.LocalClientActive, Is.True);
        }

        [Test]
        public void AddLocalConnectionExceptionTest()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                this.server.AddLocalConnection(null, null);
            });
        }



        [Test]
        public void StartedNotNullTest()
        {
            Assert.That(this.server.Started, Is.Not.Null);
        }

        [Test]
        public void ConnectedNotNullTest()
        {
            Assert.That(this.server.Connected, Is.Not.Null);
        }

        [Test]
        public void AuthenticatedNotNullTest()
        {
            Assert.That(this.server.Authenticated, Is.Not.Null);
        }

        [Test]
        public void DisconnectedNotNullTest()
        {
            Assert.That(this.server.Disconnected, Is.Not.Null);
        }

        [Test]
        public void StoppedNotNullTest()
        {
            Assert.That(this.server.Stopped, Is.Not.Null);
        }

        [Test]
        public void OnStartHostNotNullTest()
        {
            Assert.That(this.server.OnStartHost, Is.Not.Null);
        }

        [Test]
        public void OnStopHostNotNullTest()
        {
            Assert.That(this.server.OnStopHost, Is.Not.Null);
        }

        [Test]
        public void TimeNotNullTest()
        {
            Assert.That(this.server.World.Time, Is.Not.Null);
        }
    }
}

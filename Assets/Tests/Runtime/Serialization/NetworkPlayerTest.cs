using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Mirage.Tests.Runtime
{
    public class NetworkPlayerTestBase
    {
        protected NetworkPlayer player;
        protected SocketLayer.IConnection connection;

        [SetUp]
        public virtual void SetUp()
        {
            this.connection = Substitute.For<SocketLayer.IConnection>();
            this.player = new NetworkPlayer(this.connection);
        }
    }

    public class NetworkPlayerCharactorTest : NetworkPlayerTestBase
    {
        [Test]
        public void EventCalledWhenIdentityChanged()
        {
            var character = new GameObject("EventCalledWhenIdentityChanged").AddComponent<NetworkIdentity>();

            var action = Substitute.For<Action<NetworkIdentity>>();
            this.player.OnIdentityChanged += action;
            this.player.Identity = character;

            action.Received(1).Invoke(character);
            action.ClearReceivedCalls();

            this.player.Identity = null;
            action.Received(1).Invoke(null);
        }

        [Test]
        public void EventNotCalledWhenIdentityIsSame()
        {
            var character = new GameObject("EventNotCalledWhenIdentityIsSame").AddComponent<NetworkIdentity>();

            var action = Substitute.For<Action<NetworkIdentity>>();
            this.player.OnIdentityChanged += action;
            this.player.Identity = character;
            action.ClearReceivedCalls();

            // set to same value
            this.player.Identity = character;
            action.DidNotReceive().Invoke(Arg.Any<NetworkIdentity>());
        }

        [Test]
        public void HasCharacterReturnsFalseIfIdentityIsSet()
        {
            Debug.Assert(this.player.Identity == null, "player had an identity, this test is invalid");
            Assert.That(this.player.HasCharacter, Is.False);
        }

        [Test]
        public void HasCharacterReturnsTrueIfIdentityIsSet()
        {
            var character = new GameObject("HasCharacterReturnsTrueIfIdentityIsSet").AddComponent<NetworkIdentity>();

            this.player.Identity = character;

            Debug.Assert(this.player.Identity != null, "player did not have identity, this test is invalid");
            Assert.That(this.player.HasCharacter, Is.True);

            GameObject.Destroy(character.gameObject);
        }
    }

    public class NetworkPlayerMessageSendingTest : NetworkPlayerTestBase
    {
        [Test]
        [TestCase(Channel.Reliable)]
        [TestCase(Channel.Unreliable)]
        public void SendCallsSendOnConnection(int channel)
        {
            var message = new byte[] { 0, 1, 2 };
            this.player.Send(new ArraySegment<byte>(message), channel);
            if (channel == Channel.Reliable)
            {
                this.connection.Received(1).SendReliable(Arg.Is<ArraySegment<byte>>(arg => arg.SequenceEqual(message)));
            }
            else if (channel == Channel.Unreliable)
            {
                this.connection.Received(1).SendUnreliable(Arg.Is<ArraySegment<byte>>(arg => arg.SequenceEqual(message)));
            }
        }

        [Test]
        public void DisconnectCallsDisconnectOnConnection()
        {
            this.player.Disconnect();
            this.connection.Received(1).Disconnect();
        }

        [Test]
        public void DisconnectStopsMessagesBeingSentToConnection()
        {
            this.player.Disconnect();
            this.player.Send(new ArraySegment<byte>(new byte[] { 0, 1, 2 }));
            this.connection.DidNotReceive().SendReliable(Arg.Any<byte[]>());
            this.connection.DidNotReceive().SendUnreliable(Arg.Any<byte[]>());
        }
        [Test]
        public void MarkAsDisconnectedStopsMessagesBeingSentToConnection()
        {
            this.player.MarkAsDisconnected();
            this.player.Send(new ArraySegment<byte>(new byte[] { 0, 1, 2 }));
            this.connection.DidNotReceive().SendReliable(Arg.Any<byte[]>());
            this.connection.DidNotReceive().SendUnreliable(Arg.Any<byte[]>());
        }
    }
}

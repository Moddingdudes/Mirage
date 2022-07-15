using System;
using System.Linq;
using Mirage.SocketLayer;
using NSubstitute;
using NUnit.Framework;

namespace Mirage.Tests
{
    public class PipePeerConnectionTest
    {
        private class ConnectionHandler
        {
            public IDataHandler handler;
            public IConnection connection;

            public ConnectionHandler(IDataHandler handler, IConnection connection)
            {
                this.handler = handler;
                this.connection = connection;
            }

            public void SendUnreliable(byte[] data)
            {
                this.connection.SendUnreliable(data);
            }
            public void SendReliable(byte[] data)
            {
                this.connection.SendReliable(data);
            }
            public INotifyToken SendNotify(byte[] data)
            {
                return this.connection.SendNotify(data);
            }
            public void SendNotify(byte[] data, INotifyCallBack callbacks)
            {
                this.connection.SendNotify(data, callbacks);
            }
            public void ExpectData(byte[] expected)
            {
                this.handler.Received(1).ReceiveMessage(this.connection, Arg.Is<ArraySegment<byte>>(x => x.SequenceEqual(expected)));
            }
            public void ExpectNoData()
            {
                this.handler.DidNotReceiveWithAnyArgs().ReceiveMessage(default, default);
            }
        }

        private ConnectionHandler conn1;
        private ConnectionHandler conn2;
        private Action disconnect1;
        private Action disconnect2;

        [SetUp]
        public void Setup()
        {
            var handler1 = Substitute.For<IDataHandler>();
            var handler2 = Substitute.For<IDataHandler>();
            this.disconnect1 = Substitute.For<Action>();
            this.disconnect2 = Substitute.For<Action>();
            (var connection1, var connection2) = PipePeerConnection.Create(handler1, handler2, this.disconnect1, this.disconnect2);

            this.conn1 = new ConnectionHandler(handler1, connection1);
            this.conn2 = new ConnectionHandler(handler2, connection2);
        }

        [Test]
        public void ReceivesUnreliableSentData()
        {
            this.conn1.SendUnreliable(new byte[] { 1, 2, 3, 4 });

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
        }

        [Test]
        public void ReceivesUnreliableSentDataMultiple()
        {
            this.conn1.SendUnreliable(new byte[] { 1, 2, 3, 4 });
            this.conn1.SendUnreliable(new byte[] { 5, 6, 7, 8 });

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
            this.conn2.ExpectData(new byte[] { 5, 6, 7, 8 });
        }

        [Test]
        public void ReceivesReliableSentData()
        {
            this.conn1.SendReliable(new byte[] { 1, 2, 3, 4 });

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
        }

        [Test]
        public void ReceivesReliableSentDataMultiple()
        {
            this.conn1.SendReliable(new byte[] { 1, 2, 3, 4 });
            this.conn1.SendReliable(new byte[] { 5, 6, 7, 8 });

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
            this.conn2.ExpectData(new byte[] { 5, 6, 7, 8 });
        }

        [Test]
        public void ReceivesNotifySentData()
        {
            this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
        }

        [Test]
        public void ReceivesNotifySentDataMultiple()
        {
            this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });
            this.conn1.SendNotify(new byte[] { 5, 6, 7, 8 });

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
            this.conn2.ExpectData(new byte[] { 5, 6, 7, 8 });
        }

        [Test]
        public void ReceivesNotifyCallbacksSentData()
        {
            this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 }, Substitute.For<INotifyCallBack>());

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
        }

        [Test]
        public void ReceivesNotifyCallbacksSentDataMultiple()
        {
            this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 }, Substitute.For<INotifyCallBack>());
            this.conn1.SendNotify(new byte[] { 5, 6, 7, 8 }, Substitute.For<INotifyCallBack>());

            this.conn2.ExpectData(new byte[] { 1, 2, 3, 4 });
            this.conn2.ExpectData(new byte[] { 5, 6, 7, 8 });
        }

        [Test]
        public void DisconnectShouldBeCalledOnBothConnections()
        {
            this.conn1.connection.Disconnect();

            Assert.That(this.conn1.connection.State, Is.EqualTo(ConnectionState.Disconnected));
            Assert.That(this.conn2.connection.State, Is.EqualTo(ConnectionState.Disconnected));

            this.disconnect1.Received(1).Invoke();
            this.disconnect2.Received(1).Invoke();
        }

        [Test]
        public void NoReceivesUnreliableAfterDisconnect()
        {
            this.conn1.connection.Disconnect();
            this.conn1.SendUnreliable(new byte[] { 1, 2, 3, 4 });

            this.conn2.ExpectNoData();
        }

        [Test]
        public void NoReceivesReliableAfterDisconnect()
        {
            this.conn1.connection.Disconnect();
            this.conn1.SendReliable(new byte[] { 1, 2, 3, 4 });

            this.conn2.ExpectNoData();
        }

        [Test]
        public void NoReceivesNotifyAfterDisconnect()
        {
            this.conn1.connection.Disconnect();
            _ = this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });

            this.conn2.ExpectNoData();
        }

        [Test]
        public void EndpointIsPipeEndPoint()
        {
            Assert.That(this.conn1.connection.EndPoint, Is.TypeOf<PipePeerConnection.PipeEndPoint>());
        }

        [Test]
        public void NotifyTokenShouldInvokeHandlerImmediately()
        {
            var token = this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });
            Assert.That(token, Is.TypeOf<PipePeerConnection.PipeNotifyToken>());

            var invoked = 0;
            token.Delivered += () => invoked++;
            Assert.That(invoked, Is.EqualTo(1), "Delivered should be invoked 1 time Immediately when handler");
        }

        [Test]
        public void NotifyTokenShouldNotSavePreviousHandler()
        {
            var token = this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });
            Assert.That(token, Is.TypeOf<PipePeerConnection.PipeNotifyToken>());

            var invoked1 = 0;
            token.Delivered += () => invoked1++;
            Assert.That(invoked1, Is.EqualTo(1), "Delivered should be invoked 1 time Immediately when handler");

            var invoked2 = 0;
            token.Delivered += () => invoked2++;
            Assert.That(invoked1, Is.EqualTo(1), "invoked1 handler should not be called a second time");
            Assert.That(invoked2, Is.EqualTo(1));
        }

        [Test]
        public void NotifyTokenRemoveDeliveredHandlerDoesNothing()
        {
            var token = this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });
            Assert.That(token, Is.TypeOf<PipePeerConnection.PipeNotifyToken>());

            var invoked1 = 0;
            token.Delivered -= () => invoked1++;
            Assert.That(invoked1, Is.EqualTo(0), "Does nothing");
        }

        [Test]
        public void NotifyTokenAddLostHandlerDoesNothing()
        {
            var token = this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });
            Assert.That(token, Is.TypeOf<PipePeerConnection.PipeNotifyToken>());

            var invoked1 = 0;
            token.Lost += () => invoked1++;
            Assert.That(invoked1, Is.EqualTo(0), "Does nothing");
        }

        [Test]
        public void NotifyTokenRemoveLostHandlerDoesNothing()
        {
            var token = this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 });
            Assert.That(token, Is.TypeOf<PipePeerConnection.PipeNotifyToken>());

            var invoked1 = 0;
            token.Lost -= () => invoked1++;
            Assert.That(invoked1, Is.EqualTo(0), "Does nothing");
        }

        [Test]
        public void NotifyCallbackShouldBeInvokedImmediately()
        {
            var callbacks = Substitute.For<INotifyCallBack>();
            this.conn1.SendNotify(new byte[] { 1, 2, 3, 4 }, callbacks);

            callbacks.Received(1).OnDelivered();
            callbacks.DidNotReceive().OnLost();
        }
    }
}

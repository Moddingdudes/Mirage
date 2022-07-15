using System;
using Mirage.Logging;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirage.Tests.Runtime
{
    public class MessageHandlerTest
    {
        private NetworkPlayer player;
        private NetworkReader reader;
        private SocketLayer.IConnection connection;
        private MessageHandler messageHandler;

        [SetUp]
        public void SetUp()
        {
            this.connection = Substitute.For<SocketLayer.IConnection>();
            this.player = new NetworkPlayer(this.connection);
            // reader with some random data
            this.reader = new NetworkReader();
            this.reader.Reset(new byte[] { 1, 2, 3, 4 });

            this.messageHandler = new MessageHandler(null, true);
        }


        [Test]
        public void InvokesMessageHandler()
        {
            var invoked = 0;
            this.messageHandler.RegisterHandler<SceneReadyMessage>(_ => { invoked++; });

            var messageId = MessagePacker.GetId<SceneReadyMessage>();
            this.messageHandler.InvokeHandler(this.player, messageId, this.reader);

            Assert.That(invoked, Is.EqualTo(1), "Should have been invoked");
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void DisconnectsIfHandlerHasException(bool disconnectOnThrow)
        {
            this.messageHandler = new MessageHandler(null, disconnectOnThrow);

            var invoked = 0;
            this.messageHandler.RegisterHandler<SceneReadyMessage>(_ => { invoked++; throw new InvalidOperationException("Fun Exception"); });

            var packet = new ArraySegment<byte>(MessagePacker.Pack(new SceneReadyMessage()));
            LogAssert.ignoreFailingMessages = true;
            Assert.DoesNotThrow(() =>
            {
                this.messageHandler.HandleMessage(this.player, packet);
            });
            LogAssert.ignoreFailingMessages = false;

            Assert.That(invoked, Is.EqualTo(1), "Should have been invoked");

            if (disconnectOnThrow)
            {
                this.connection.Received(1).Disconnect();
            }
            else
            {
                this.connection.DidNotReceive().Disconnect();
            }
        }

        [Test]
        public void LogsWhenNoHandlerIsFound()
        {
            this.ExpectLog(() =>
            {
                var messageId = MessagePacker.GetId<SceneMessage>();
                this.messageHandler.InvokeHandler(this.player, messageId, this.reader);
            }
            , LogType.Warning, $"Unexpected message {typeof(SceneMessage)} received from {this.player}. Did you register a handler for it?");
        }

        [Test]
        public void LogsWhenUnknownMessage()
        {
            const int id = 1234;
            this.ExpectLog(() =>
            {
                this.messageHandler.InvokeHandler(this.player, id, this.reader);
            }
            , LogType.Log, $"Unexpected message ID {id} received from {this.player}. May be due to no existing RegisterHandler for this message.");
        }

        private void ExpectLog(Action action, LogType type, string log)
        {
            var logger = LogFactory.GetLogger(typeof(MessageHandler));
            var existing = logger.logHandler;
            var existingLevel = logger.filterLogType;
            try
            {
                var handler = Substitute.For<ILogHandler>();
                logger.logHandler = handler;
                logger.filterLogType = LogType.Log;

                action.Invoke();

                handler.Received(1).LogFormat(type, null, "{0}", log);
            }
            finally
            {
                logger.logHandler = existing;
                logger.filterLogType = existingLevel;
            }
        }
    }
}

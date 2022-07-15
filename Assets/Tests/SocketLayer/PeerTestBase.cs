using System;
using Mirage.Logging;
using Mirage.Tests;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Mirage.SocketLayer.Tests.PeerTests
{
    /// <summary>
    /// base class of PeerTests that has setup
    /// </summary>
    public class PeerTestBase
    {
        public const int maxConnections = 5;
        public const int MAX_PACKET_SIZE = 1300;

        protected byte[] connectRequest;
        private PeerInstance instance;
        protected Action<IConnection> connectAction;
        protected Action<IConnection, RejectReason> connectFailedAction;
        protected Action<IConnection, DisconnectReason> disconnectAction;

        // helper properties to access instance
        protected ISocket socket => this.instance.socket;
        protected IDataHandler dataHandler => this.instance.dataHandler;
        protected Config config => this.instance.config;
        protected ILogger logger => this.instance.logger;
        protected Peer peer => this.instance.peer;

        internal readonly Time time = new Time();

        [SetUp]
        public void SetUp()
        {
            this.instance = new PeerInstance();

            this.connectAction = Substitute.For<Action<IConnection>>();
            this.connectFailedAction = Substitute.For<Action<IConnection, RejectReason>>();
            this.disconnectAction = Substitute.For<Action<IConnection, DisconnectReason>>();
            this.peer.OnConnected += this.connectAction;
            this.peer.OnConnectionFailed += this.connectFailedAction;
            this.peer.OnDisconnected += this.disconnectAction;

            this.CreateConnectPacket();
        }

        private void CreateConnectPacket()
        {
            var keyValidator = new ConnectKeyValidator(this.instance.config.key);
            this.connectRequest = new byte[2 + keyValidator.KeyLength];
            this.connectRequest[0] = (byte)PacketType.Command;
            this.connectRequest[1] = (byte)Commands.ConnectRequest;
            keyValidator.CopyTo(this.connectRequest);
        }
    }

    /// <summary>
    /// Peer and Substitutes for test
    /// </summary>
    public class PeerInstance
    {
        public ISocket socket;
        public IDataHandler dataHandler;
        public Config config;
        public ILogger logger;
        public Peer peer;

        public PeerInstance(Config config = null, ISocket socket = null)
        {
            this.socket = socket ?? Substitute.For<ISocket>();
            this.dataHandler = Substitute.For<IDataHandler>();

            this.config = config ?? new Config()
            {
                MaxConnections = PeerTestBase.maxConnections,
                // 1 second before "failed to connect"
                MaxConnectAttempts = 5,
                ConnectAttemptInterval = 0.2f,
            };
            this.logger = LogFactory.GetLogger<PeerInstance>();
            this.peer = new Peer(this.socket, PeerTestBase.MAX_PACKET_SIZE, this.dataHandler, this.config, this.logger);
        }
    }

    /// <summary>
    /// Peer and Substitutes for testing but with TestSocket
    /// </summary>
    public class PeerInstanceWithSocket : PeerInstance
    {
        public new TestSocket socket;
        /// <summary>
        /// endpoint that other sockets use to send to this
        /// </summary>
        public IEndPoint endPoint;

        public PeerInstanceWithSocket(Config config = null) : base(config, socket: new TestSocket("TestInstance"))
        {
            this.socket = (TestSocket)base.socket;
            this.endPoint = this.socket.endPoint;
        }
    }

    public static class ArgCollection
    {
        public static bool AreEquivalentIgnoringLength<T>(this T[] actual, T[] expected) where T : IEquatable<T>
        {
            // atleast same length
            if (actual.Length < expected.Length)
            {
                Debug.LogError($"length of actual was less than expected\n" +
                    $"  actual length:{actual.Length}\n" +
                    $"  expected length:{expected.Length}");
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (!actual[i].Equals(expected[i]))
                {
                    Debug.LogError($"element {i} in actual was not equal to expected\n" +
                        $"  actual[{i}]:{actual[i]}\n" +
                        $"  expected[{i}]:{expected[i]}");
                    return false;
                }
            }

            return true;
        }

        public static void SetupReceiveCall(this ISocket socket, byte[] data, IEndPoint endPoint = null, int? length = null)
        {
            socket.Poll().Returns(true, false);
            socket
               // when any call
               .When(x => x.Receive(Arg.Any<byte[]>(), out Arg.Any<IEndPoint>()))
               // return the data from endpoint
               .Do(x =>
               {
                   var dataArg = (byte[])x[0];
                   for (var i = 0; i < data.Length; i++)
                   {
                       dataArg[i] = data[i];
                   }
                   x[1] = endPoint ?? TestEndPoint.CreateSubstitute();
               });
            socket.Receive(Arg.Any<byte[]>(), out Arg.Any<IEndPoint>()).Returns(length ?? data.Length);
        }
    }
}

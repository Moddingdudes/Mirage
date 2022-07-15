using System;
using Mirage.Tests;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Mirage.SocketLayer.Tests.PeerTests
{
    [Category("SocketLayer"), Description("tests for Peer that only apply to server")]
    public class PeerTestAsServer : PeerTestBase
    {
        [Test]
        public void BindShoudlCallSocketBind()
        {
            var endPoint = TestEndPoint.CreateSubstitute();
            this.peer.Bind(endPoint);

            this.socket.Received(1).Bind(Arg.Is(endPoint));
        }

        [Test]
        public void CloseSendsDisconnectMessageToAllConnections()
        {
            var endPoint = TestEndPoint.CreateSubstitute();
            this.peer.Bind(endPoint);

            var endPoints = new IEndPoint[maxConnections];
            for (var i = 0; i < maxConnections; i++)
            {
                endPoints[i] = TestEndPoint.CreateSubstitute();

                this.socket.SetupReceiveCall(this.connectRequest, endPoints[i]);
                this.peer.UpdateTest();
            }

            for (var i = 0; i < maxConnections; i++)
            {
                this.socket.ClearReceivedCalls();
            }

            this.peer.Close();

            var disconnectCommand = new byte[3]
            {
                (byte)PacketType.Command,
                (byte)Commands.Disconnect,
                (byte)DisconnectReason.RequestedByRemotePeer,
            };
            for (var i = 0; i < maxConnections; i++)
            {
                this.socket.Received(1).Send(
                    Arg.Is(endPoints[i]),
                    Arg.Is<byte[]>(actual => actual.AreEquivalentIgnoringLength(disconnectCommand)),
                    Arg.Is(disconnectCommand.Length)
                );
            }
        }

        [Test]
        public void AcceptsConnectionForValidMessage()
        {
            this.peer.Bind(TestEndPoint.CreateSubstitute());

            var connectAction = Substitute.For<Action<IConnection>>();
            this.peer.OnConnected += connectAction;

            var endPoint = TestEndPoint.CreateSubstitute();
            this.socket.SetupReceiveCall(this.connectRequest, endPoint);
            this.peer.UpdateTest();

            // server sends accept and invokes event locally
            this.socket.Received(1).Send(endPoint, Arg.Is<byte[]>(x =>
                x.Length >= 2 &&
                x[0] == (byte)PacketType.Command &&
                x[1] == (byte)Commands.ConnectionAccepted
            ), 2);
            connectAction.ReceivedWithAnyArgs(1).Invoke(default);
        }

        [Test]
        public void AcceptsConnectionsUpToMax()
        {
            this.peer.Bind(TestEndPoint.CreateSubstitute());

            var connectAction = Substitute.For<Action<IConnection>>();
            this.peer.OnConnected += connectAction;


            var endPoints = new IEndPoint[maxConnections];
            for (var i = 0; i < maxConnections; i++)
            {
                endPoints[i] = TestEndPoint.CreateSubstitute();

                this.socket.SetupReceiveCall(this.connectRequest, endPoints[i]);
                this.peer.UpdateTest();
            }


            // server sends accept and invokes event locally
            connectAction.ReceivedWithAnyArgs(maxConnections).Invoke(default);
            for (var i = 0; i < maxConnections; i++)
            {
                this.socket.Received(1).Send(endPoints[i], Arg.Is<byte[]>(x =>
                    x.Length >= 2 &&
                    x[0] == (byte)PacketType.Command &&
                    x[1] == (byte)Commands.ConnectionAccepted
                ), 2);
            }
        }

        [Test]
        public void RejectsConnectionOverMax()
        {
            this.peer.Bind(TestEndPoint.CreateSubstitute());

            var connectAction = Substitute.For<Action<IConnection>>();
            this.peer.OnConnected += connectAction;

            for (var i = 0; i < maxConnections; i++)
            {
                this.socket.SetupReceiveCall(this.connectRequest);
                this.peer.UpdateTest();
            }

            // clear calls from valid connections
            this.socket.ClearReceivedCalls();
            connectAction.ClearReceivedCalls();

            var overMaxEndpoint = TestEndPoint.CreateSubstitute();
            this.socket.SetupReceiveCall(this.connectRequest, overMaxEndpoint);


            byte[] received = null;
            this.socket.WhenForAnyArgs(x => x.Send(default, default, default)).Do(x =>
            {
                received = (byte[])x[1];
            });

            this.peer.UpdateTest();

            Debug.Log($"Length:{received.Length} [{received[0]},{received[1]},{received[2]}]");
            const int length = 3;
            this.socket.Received(1).Send(overMaxEndpoint, Arg.Is<byte[]>(x =>
                x.Length >= length &&
                x[0] == (byte)PacketType.Command &&
                x[1] == (byte)Commands.ConnectionRejected &&
                x[2] == (byte)RejectReason.ServerFull
            ), length);
            connectAction.DidNotReceiveWithAnyArgs().Invoke(default);
        }
    }
}

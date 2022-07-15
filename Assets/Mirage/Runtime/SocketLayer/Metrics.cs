namespace Mirage.SocketLayer
{
    public class Metrics
    {
        public readonly Sequencer Sequencer;
        public readonly Frame[] buffer;
        public uint tick;
        [System.Obsolete("Frame is now a struct, use buffer and tick instead if you need to set data")]
        public Frame Current => this.buffer[this.tick];

        public Metrics(int bitSize = 10)
        {
            this.buffer = new Frame[1 << bitSize];
            this.Sequencer = new Sequencer(bitSize);
        }

        public void OnTick(int connectionCount)
        {
            this.tick = (uint)this.Sequencer.NextAfter(this.tick);
            this.buffer[this.tick] = Frame.CreateNew();
            this.buffer[this.tick].connectionCount = connectionCount;
        }

        public void OnSend(int length)
        {
            this.buffer[this.tick].sendCount++;
            this.buffer[this.tick].sendBytes += length;
        }

        internal void OnSendUnconnected(int length)
        {
            this.buffer[this.tick].sendUnconnectedCount++;
            this.buffer[this.tick].sendUnconnectedBytes += length;
        }

        public void OnResend(int length)
        {
            this.buffer[this.tick].resendCount++;
            this.buffer[this.tick].resendBytes += length;
        }

        public void OnReceive(int length)
        {
            this.buffer[this.tick].receiveCount++;
            this.buffer[this.tick].receiveBytes += length;
        }

        public void OnReceiveUnconnected(int length)
        {
            this.buffer[this.tick].receiveUnconnectedCount++;
            this.buffer[this.tick].receiveUnconnectedBytes += length;
        }

        public void OnSendMessageUnreliable(int length)
        {
            this.buffer[this.tick].sendMessagesUnreliableCount++;
            this.buffer[this.tick].sendMessagesUnreliableBytes += length;
        }

        public void OnReceiveMessageUnreliable(int length)
        {
            this.buffer[this.tick].receiveMessagesUnreliableCount++;
            this.buffer[this.tick].receiveMessagesUnreliableBytes += length;
        }

        public void OnSendMessageReliable(int length)
        {
            this.buffer[this.tick].sendMessagesReliableCount++;
            this.buffer[this.tick].sendMessagesReliableBytes += length;
        }

        public void OnReceiveMessageReliable(int length)
        {
            this.buffer[this.tick].receiveMessagesReliableCount++;
            this.buffer[this.tick].receiveMessagesReliableBytes += length;
        }

        public void OnSendMessageNotify(int length)
        {
            this.buffer[this.tick].sendMessagesNotifyCount++;
            this.buffer[this.tick].sendMessagesNotifyBytes += length;
        }

        public void OnReceiveMessageNotify(int length)
        {
            this.buffer[this.tick].receiveMessagesNotifyCount++;
            this.buffer[this.tick].receiveMessagesNotifyBytes += length;
        }

        public struct Frame
        {
            /// <summary>
            /// Clears frame ready to be used
            /// <para>Default will have init has false so can be used to exclude frames that are not used yet</para>
            /// <para>Use this function to create a new frame with init set to true</para>
            /// </summary>
            internal static Frame CreateNew()
            {
                return new Frame { init = true };
            }

            /// <summary>Is this frame initialized (uninitialized frames can be excluded from averages)</summary>
            public bool init;

            /// <summary>Number of connections</summary>
            public int connectionCount;

            /// <summary>Number of send calls to connections</summary>
            public int sendCount;
            /// <summary>Number of bytes sent to connections</summary>
            public int sendBytes;

            /// <summary>Number of resend calls by reliable system</summary>
            public int resendCount;
            /// <summary>Number of bytes resent by reliable system</summary>
            public int resendBytes;

            /// <summary>Number of packets received from connections</summary>
            public int receiveCount;
            /// <summary>Number of bytes received from connections</summary>
            public int receiveBytes;

            #region Unconnected
            /// <summary>Number of send calls to unconnected addresses</summary>
            public int sendUnconnectedCount;
            /// <summary>Number of bytes sent to unconnected addresses</summary>
            public int sendUnconnectedBytes;

            /// <summary>Number of packets received from unconnected addresses</summary>
            public int receiveUnconnectedBytes;
            /// <summary>Number of bytes received from unconnected addresses</summary>
            public int receiveUnconnectedCount;
            #endregion

            #region Messages
            /// <summary>Number of Unreliable message sent to connections</summary>
            public int sendMessagesUnreliableCount;
            /// <summary>Number of Unreliable bytes sent to connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int sendMessagesUnreliableBytes;

            /// <summary>Number of Unreliable message received from connections</summary>
            public int receiveMessagesUnreliableCount;
            /// <summary>Number of Unreliable bytes received from connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int receiveMessagesUnreliableBytes;

            /// <summary>Number of Reliable message sent to connections</summary>
            public int sendMessagesReliableCount;
            /// <summary>Number of Reliable bytes sent to connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int sendMessagesReliableBytes;

            /// <summary>Number of Reliable message received from connections</summary>
            public int receiveMessagesReliableCount;
            /// <summary>Number of Reliable bytes received from connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int receiveMessagesReliableBytes;

            /// <summary>Number of Notify message sent to connections</summary>
            public int sendMessagesNotifyCount;
            /// <summary>Number of Notify bytes sent to connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int sendMessagesNotifyBytes;

            /// <summary>Number of Notify message received from connections</summary>
            public int receiveMessagesNotifyCount;
            /// <summary>Number of Notify bytes received from connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int receiveMessagesNotifyBytes;

            /// <summary>Number of message sent to connections</summary>
            public int sendMessagesCountTotal => this.sendMessagesUnreliableCount + this.sendMessagesReliableCount + this.sendMessagesNotifyCount;
            /// <summary>Number of bytes sent to connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int sendMessagesBytesTotal => this.sendMessagesUnreliableBytes + this.sendMessagesReliableBytes + this.sendMessagesNotifyBytes;

            /// <summary>Number of message received from connections</summary>
            public int receiveMessagesCountTotal => this.receiveMessagesUnreliableCount + this.receiveMessagesReliableCount + this.receiveMessagesNotifyCount;
            /// <summary>Number of bytes received from connections (excludes packets headers, will just be the message sent by high level)</summary>
            public int receiveMessagesBytesTotal => this.receiveMessagesUnreliableBytes + this.receiveMessagesReliableBytes + this.receiveMessagesNotifyBytes;
            #endregion
        }
    }
}

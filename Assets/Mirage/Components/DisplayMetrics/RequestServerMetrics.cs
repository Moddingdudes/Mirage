using System;
using System.Collections;
using System.Collections.Generic;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage.DisplayMetrics
{
    public class RequestServerMetrics : MonoBehaviour
    {
        public NetworkServer server;
        public NetworkClient client;
        public DisplayMetricsAverageGui displayMetrics;
        public bool RequestMetrics = false;

        /// <summary>
        /// Connections that are requesting metrics
        /// </summary>
        private HashSet<INetworkPlayer> connections;
        private Metrics metrics;
        private uint lastSendTick;

        private void Start()
        {
            if (this.RequestMetrics)
            {
                this.client.Connected.AddListener((x) => this.sendRequest());
            }

            this.server.Started.AddListener(this.ServerStarted);
            this.StartCoroutine(this.Runner());
        }

        private void ServerStarted()
        {
            this.connections = new HashSet<INetworkPlayer>();

            this.server.MessageHandler.RegisterHandler<RequestMetricsMessage>(this.OnRequestMetricsMessage);
            this.server.Disconnected.AddListener(x => this.connections.Remove(x));
        }

        private void OnRequestMetricsMessage(INetworkPlayer arg1, RequestMetricsMessage arg2)
        {
            this.connections.Add(arg1);
            if (this.metrics == null)
            {
                this.metrics = this.server.Metrics;
                this.lastSendTick = this.metrics.tick;
            }
        }

        private void sendRequest()
        {
            this.client.MessageHandler.RegisterHandler<SendMetricsMessage>(this.OnSendMetricsMessage);
            this.client.Player.Send(new RequestMetricsMessage());
            this.metrics = new Metrics();
            this.displayMetrics.Metrics = this.metrics;
        }

        private void OnSendMetricsMessage(INetworkPlayer _, SendMetricsMessage msg)
        {
            for (uint i = 0; i < msg.newFrames.Length; i++)
            {
                var seq = this.metrics.Sequencer.MoveInBounds(i + msg.start);
                this.metrics.buffer[seq] = msg.newFrames[i];
            }
        }

        [NetworkMessage]
        private struct RequestMetricsMessage
        {

        }

        [NetworkMessage]
        private struct SendMetricsMessage
        {
            public uint start;
            public Metrics.Frame[] newFrames;
        }

        public IEnumerator Runner()
        {
            while (true)
            {
                try
                {
                    if (this.server.Active && this.connections.Count > 0) this.ServerUpdate();
                    if (this.RequestMetrics && this.client.Active) this.ClientUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
        private void ServerUpdate()
        {
            var msg = new SendMetricsMessage
            {
                start = lastSendTick,
                newFrames = this.getFrames(this.lastSendTick, this.metrics.tick)
            };

            foreach (var player in this.connections)
            {
                player.Send(msg);
            }
        }

        private Metrics.Frame[] getFrames(uint start, uint end)
        {
            var count = this.metrics.Sequencer.Distance(end, start);
            // limit to 100 frames
            if (count > 100) count = 100;

            var frames = new Metrics.Frame[count];
            for (uint i = 0; i < count; i++)
            {
                var seq = this.metrics.Sequencer.MoveInBounds(i + start);
                frames[i] = this.metrics.buffer[seq];
            }

            this.lastSendTick = (uint)this.metrics.Sequencer.MoveInBounds(start + (uint)count);

            return frames;
        }

        private void ClientUpdate()
        {

        }
    }
}

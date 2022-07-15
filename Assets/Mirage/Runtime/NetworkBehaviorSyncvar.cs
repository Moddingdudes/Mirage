using Mirage.Serialization;

namespace Mirage
{

    /// <summary>
    /// backing struct for a NetworkIdentity when used as a syncvar
    /// the weaver will replace the syncvar with this struct.
    /// </summary>
    public struct NetworkBehaviorSyncvar
    {
        /// <summary>
        /// The network client that spawned the parent object
        /// used to lookup the identity if it exists
        /// </summary>
        internal IObjectLocator objectLocator;
        internal uint netId;
        internal int componentId;

        internal NetworkBehaviour component;

        internal uint NetId => this.component != null ? this.component.NetId : this.netId;
        internal int ComponentId => this.component != null ? this.component.ComponentIndex : this.componentId;

        public NetworkBehaviour Value
        {
            get
            {
                if (this.component != null)
                    return this.component;

                if (this.objectLocator != null && this.objectLocator.TryGetIdentity(this.NetId, out var result))
                {
                    return result.NetworkBehaviours[this.componentId];
                }


                return null;
            }

            set
            {
                if (value == null)
                {
                    this.netId = 0;
                    this.componentId = 0;
                }
                this.component = value;
            }
        }
    }


    public static class NetworkBehaviorSerializers
    {
        public static void WriteNetworkBehaviorSyncVar(this NetworkWriter writer, NetworkBehaviorSyncvar id)
        {
            writer.WritePackedUInt32(id.NetId);
            writer.WritePackedInt32(id.ComponentId);
        }

        public static NetworkBehaviorSyncvar ReadNetworkBehaviourSyncVar(this NetworkReader reader)
        {
            var mirageReader = reader.ToMirageReader();

            var netId = reader.ReadPackedUInt32();
            var componentId = reader.ReadPackedInt32();

            NetworkIdentity identity = null;
            var hasValue = mirageReader.ObjectLocator?.TryGetIdentity(netId, out identity) ?? false;

            return new NetworkBehaviorSyncvar
            {
                objectLocator = mirageReader.ObjectLocator,
                netId = netId,
                componentId = componentId,
                component = hasValue ? identity.NetworkBehaviours[componentId] : null
            };
        }
    }
}

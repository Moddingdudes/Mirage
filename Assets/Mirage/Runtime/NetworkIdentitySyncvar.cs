using Mirage.Serialization;

namespace Mirage
{

    /// <summary>
    /// backing struct for a NetworkIdentity when used as a syncvar
    /// the weaver will replace the syncvar with this struct.
    /// </summary>
    public struct NetworkIdentitySyncvar
    {
        /// <summary>
        /// The network client that spawned the parent object
        /// used to lookup the identity if it exists
        /// </summary>
        internal IObjectLocator objectLocator;
        internal uint netId;

        internal NetworkIdentity identity;

        internal uint NetId => this.identity != null ? this.identity.NetId : this.netId;

        public NetworkIdentity Value
        {
            get
            {
                if (this.identity != null)
                    return this.identity;

                if (this.objectLocator != null && this.objectLocator.TryGetIdentity(this.NetId, out var result))
                {
                    return result;
                }

                return null;
            }

            set
            {
                if (value == null)
                    this.netId = 0;
                this.identity = value;
            }
        }
    }


    public static class NetworkIdentitySerializers
    {
        public static void WriteNetworkIdentitySyncVar(this NetworkWriter writer, NetworkIdentitySyncvar id)
        {
            writer.WritePackedUInt32(id.NetId);
        }

        public static NetworkIdentitySyncvar ReadNetworkIdentitySyncVar(this NetworkReader reader)
        {
            var mirageReader = reader.ToMirageReader();

            var netId = reader.ReadPackedUInt32();

            NetworkIdentity identity = null;
            mirageReader.ObjectLocator?.TryGetIdentity(netId, out identity);

            return new NetworkIdentitySyncvar
            {
                objectLocator = mirageReader.ObjectLocator,
                netId = netId,
                identity = identity
            };
        }
    }
}

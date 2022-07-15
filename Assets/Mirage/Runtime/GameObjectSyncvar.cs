using Mirage.Serialization;
using UnityEngine;

namespace Mirage
{

    /// <summary>
    /// backing struct for a NetworkIdentity when used as a syncvar
    /// the weaver will replace the syncvar with this struct.
    /// </summary>
    public struct GameObjectSyncvar
    {
        /// <summary>
        /// The network client that spawned the parent object
        /// used to lookup the identity if it exists
        /// </summary>
        internal IObjectLocator objectLocator;
        internal uint netId;

        internal GameObject gameObject;

        internal uint NetId => this.gameObject != null ? this.gameObject.GetComponent<NetworkIdentity>().NetId : this.netId;

        public GameObject Value
        {
            get
            {
                if (this.gameObject != null)
                    return this.gameObject;

                if (this.objectLocator != null && this.objectLocator.TryGetIdentity(this.NetId, out var result))
                {
                    return result.gameObject;
                }

                return null;
            }

            set
            {
                if (value == null)
                    this.netId = 0;
                this.gameObject = value;
            }
        }
    }

    public static class GameObjectSerializers
    {
        public static void WriteGameObjectSyncVar(this NetworkWriter writer, GameObjectSyncvar id)
        {
            writer.WritePackedUInt32(id.NetId);
        }

        public static GameObjectSyncvar ReadGameObjectSyncVar(this NetworkReader reader)
        {
            var mirageReader = reader.ToMirageReader();

            var netId = reader.ReadPackedUInt32();

            NetworkIdentity identity = null;
            var hasValue = mirageReader.ObjectLocator?.TryGetIdentity(netId, out identity) ?? false;

            return new GameObjectSyncvar
            {
                objectLocator = mirageReader.ObjectLocator,
                netId = netId,
                gameObject = hasValue ? identity.gameObject : null
            };
        }
    }
}

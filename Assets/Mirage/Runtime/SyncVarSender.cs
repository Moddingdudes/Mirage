using System.Collections.Generic;

namespace Mirage
{
    /// <summary>
    /// Class that Syncs syncvar and other <see cref="NetworkIdentity"/> State
    /// </summary>
    public class SyncVarSender
    {
        private readonly HashSet<NetworkIdentity> DirtyObjects = new HashSet<NetworkIdentity>();
        private readonly List<NetworkIdentity> DirtyObjectsTmp = new List<NetworkIdentity>();

        public void AddDirtyObject(NetworkIdentity dirty)
        {
            this.DirtyObjects.Add(dirty);
        }


        internal void Update()
        {
            this.DirtyObjectsTmp.Clear();

            foreach (var identity in this.DirtyObjects)
            {
                if (identity != null)
                {
                    identity.UpdateVars();

                    if (identity.StillDirty())
                        this.DirtyObjectsTmp.Add(identity);
                }
            }

            this.DirtyObjects.Clear();

            foreach (var obj in this.DirtyObjectsTmp)
                this.DirtyObjects.Add(obj);
        }
    }
}

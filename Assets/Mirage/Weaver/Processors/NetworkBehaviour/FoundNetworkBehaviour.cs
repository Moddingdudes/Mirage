using System.Collections.Generic;
using Mirage.Weaver.SyncVars;
using Mono.Cecil;

namespace Mirage.Weaver.NetworkBehaviours
{
    internal class FoundNetworkBehaviour
    {
        public readonly ModuleDefinition Module;
        public readonly TypeDefinition TypeDefinition;
        public readonly ConstFieldTracker syncVarCounter;

        public FoundNetworkBehaviour(ModuleDefinition module, TypeDefinition td)
        {
            this.Module = module;
            this.TypeDefinition = td;

            this.syncVarCounter = new ConstFieldTracker("SYNC_VAR_COUNT", td, 64, "[SyncVar]");
        }

        public List<FoundSyncVar> SyncVars { get; private set; } = new List<FoundSyncVar>();

        public FoundSyncVar AddSyncVar(FieldDefinition fd)
        {
            var dirtyIndex = this.syncVarCounter.GetInBase() + this.SyncVars.Count;
            var syncVar = new FoundSyncVar(this.Module, this, fd, dirtyIndex);
            this.SyncVars.Add(syncVar);
            return syncVar;
        }

        public void SetSyncVarCount()
        {
            this.syncVarCounter.Set(this.SyncVars.Count);
        }

        public bool HasManualSerializeOverride()
        {
            return this.TypeDefinition.GetMethod(SerializeHelper.MethodName) != null;
        }
        public bool HasManualDeserializeOverride()
        {
            return this.TypeDefinition.GetMethod(DeserializeHelper.MethodName) != null;
        }
    }
}

using System;
using Mirage.Serialization;
using Mirage.Weaver.SyncVars;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Mirage.Weaver.NetworkBehaviours
{
    internal class DeserializeHelper
    {
        public const string MethodName = nameof(NetworkBehaviour.DeserializeSyncVars);
        private readonly ModuleDefinition module;
        private readonly FoundNetworkBehaviour behaviour;
        private ILProcessor worker;

        public MethodDefinition Method { get; private set; }
        public ParameterDefinition ReaderParameter { get; private set; }
        public ParameterDefinition InitializeParameter { get; private set; }
        /// <summary>
        /// IMPORTANT: this mask is only for this NB, it is not shifted based on base class
        /// </summary>
        public VariableDefinition DirtyBitsLocal { get; private set; }

        public DeserializeHelper(ModuleDefinition module, FoundNetworkBehaviour behaviour)
        {
            this.module = module;
            this.behaviour = behaviour;
        }

        /// <summary>
        /// Adds Serialize method to current type
        /// </summary>
        /// <returns></returns>
        public ILProcessor AddMethod()
        {
            this.Method = this.behaviour.TypeDefinition.AddMethod(MethodName,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);

            this.ReaderParameter = this.Method.AddParam<NetworkReader>("reader");
            this.InitializeParameter = this.Method.AddParam<bool>("initialState");

            this.Method.Body.InitLocals = true;
            this.worker = this.Method.Body.GetILProcessor();
            return this.worker;
        }

        public void AddLocals()
        {
            this.DirtyBitsLocal = this.Method.AddLocal<ulong>();
        }

        public void WriteBaseCall()
        {
            var baseDeserialize = this.behaviour.TypeDefinition.BaseType.GetMethodInBaseType(MethodName);
            if (baseDeserialize != null)
            {
                // base
                this.worker.Append(this.worker.Create(OpCodes.Ldarg_0));
                // reader
                this.worker.Append(this.worker.Create(OpCodes.Ldarg, this.ReaderParameter));
                // initialState
                this.worker.Append(this.worker.Create(OpCodes.Ldarg, this.InitializeParameter));
                this.worker.Append(this.worker.Create(OpCodes.Call, this.module.ImportReference(baseDeserialize)));
            }
        }

        public void WriteIfInitial(Action body)
        {
            // Generates: if (initial)
            var initialStateLabel = this.worker.Create(OpCodes.Nop);

            this.worker.Append(this.worker.Create(OpCodes.Ldarg, this.InitializeParameter));
            this.worker.Append(this.worker.Create(OpCodes.Brfalse, initialStateLabel));

            body.Invoke();

            this.worker.Append(this.worker.Create(OpCodes.Ret));

            // Generates: end if (initial)
            this.worker.Append(initialStateLabel);
        }

        /// <summary>
        /// Writes Reads dirty bit mask for this NB,
        /// <para>Shifts by number of syncvars in base class, then writes number of bits in this class</para>
        /// </summary>
        public void ReadDirtyBitMask()
        {
            var readBitsMethod = this.module.ImportReference(this.ReaderParameter.ParameterType.Resolve().GetMethod(nameof(NetworkReader.Read)));

            // Generates: reader.Read(n)
            // n is syncvars in this

            // get dirty bits
            this.worker.Append(this.worker.Create(OpCodes.Ldarg, this.ReaderParameter));
            this.worker.Append(this.worker.Create(OpCodes.Ldc_I4, this.behaviour.SyncVars.Count));
            this.worker.Append(this.worker.Create(OpCodes.Call, readBitsMethod));
            this.worker.Append(this.worker.Create(OpCodes.Stloc, this.DirtyBitsLocal));
        }

        internal void WriteIfSyncVarDirty(FoundSyncVar syncVar, Action body)
        {
            var endIf = this.worker.Create(OpCodes.Nop);

            // we dont shift read bits, so we have to shift dirty bit here
            var syncVarIndex = syncVar.DirtyBit >> this.behaviour.syncVarCounter.GetInBase();

            // check if dirty bit is set
            this.worker.Append(this.worker.Create(OpCodes.Ldloc, this.DirtyBitsLocal));
            this.worker.Append(this.worker.Create(OpCodes.Ldc_I8, syncVarIndex));
            this.worker.Append(this.worker.Create(OpCodes.And));
            this.worker.Append(this.worker.Create(OpCodes.Brfalse, endIf));

            body.Invoke();

            this.worker.Append(endIf);
        }
    }
}

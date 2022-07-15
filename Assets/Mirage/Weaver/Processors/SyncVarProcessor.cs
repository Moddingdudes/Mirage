using System;
using Mirage.Weaver.NetworkBehaviours;
using Mirage.Weaver.Serialization;
using Mirage.Weaver.SyncVars;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;

namespace Mirage.Weaver
{
    /// <summary>
    /// Processes [SyncVar] in NetworkBehaviour
    /// </summary>
    public class SyncVarProcessor
    {
        private readonly ModuleDefinition module;
        private readonly Readers readers;
        private readonly Writers writers;
        private readonly PropertySiteProcessor propertySiteProcessor;

        private FoundNetworkBehaviour behaviour;

        public SyncVarProcessor(ModuleDefinition module, Readers readers, Writers writers, PropertySiteProcessor propertySiteProcessor)
        {
            this.module = module;
            this.readers = readers;
            this.writers = writers;
            this.propertySiteProcessor = propertySiteProcessor;
        }

        public void ProcessSyncVars(TypeDefinition td, IWeaverLogger logger)
        {
            this.behaviour = new FoundNetworkBehaviour(this.module, td);
            // the mapping of dirtybits to sync-vars is implicit in the order of the fields here. this order is recorded in m_replacementProperties.
            // start assigning syncvars at the place the base class stopped, if any

            // find syncvars
            // use ToArray to create copy, ProcessSyncVar might add new fields
            foreach (var fd in td.Fields.ToArray())
            {
                // try/catch for each field, and log once
                // we dont want to spam multiple logs for a single field
                try
                {
                    if (this.IsValidSyncVar(fd))
                    {
                        var syncVar = this.behaviour.AddSyncVar(fd);
                        this.ProcessSyncVar(syncVar);
                        syncVar.HasProcessed = true;
                    }
                }
                catch (ValueSerializerException e)
                {
                    logger.Error(e.Message, fd);
                }
                catch (SyncVarException e)
                {
                    logger.Error(e);
                }
                catch (SerializeFunctionException e)
                {
                    // use field as member referecne
                    logger.Error(e.Message, fd);
                }
            }

            this.behaviour.SetSyncVarCount();

            this.GenerateSerialization();
            this.GenerateDeserialization();
        }

        private bool IsValidSyncVar(FieldDefinition field)
        {
            if (!field.HasCustomAttribute<SyncVarAttribute>())
            {
                return false;
            }

            if ((field.Attributes & FieldAttributes.Static) != 0)
            {
                throw new SyncVarException($"{field.Name} cannot be static", field);
            }

            if (field.FieldType.IsArray)
            {
                // todo should arrays really be blocked?
                throw new SyncVarException($"{field.Name} has invalid type. Use SyncLists instead of arrays", field);
            }

            if (SyncObjectProcessor.ImplementsSyncObject(field.FieldType))
            {
                throw new SyncVarException($"{field.Name} has [SyncVar] attribute. ISyncObject should not be marked with SyncVar", field);
            }

            return true;
        }

        private void ProcessSyncVar(FoundSyncVar syncVar)
        {
            // process attributes first before creating setting, otherwise it wont know about hook
            syncVar.SetWrapType();
            syncVar.ProcessAttributes(this.writers, this.readers);

            var fd = syncVar.FieldDefinition;

            var originalName = fd.Name;
            Weaver.DebugLog(fd.DeclaringType, $"Sync Var {fd.Name} {fd.FieldType}");

            var get = this.GenerateSyncVarGetter(syncVar);
            var set = syncVar.InitialOnly
                ? this.GenerateSyncVarSetterInitialOnly(syncVar)
                : this.GenerateSyncVarSetter(syncVar);

            //NOTE: is property even needed? Could just use a setter function?
            //create the property
            var propertyDefinition = new PropertyDefinition("Network" + originalName, PropertyAttributes.None, syncVar.OriginalType)
            {
                GetMethod = get,
                SetMethod = set
            };

            propertyDefinition.DeclaringType = fd.DeclaringType;
            //add the methods and property to the type.
            fd.DeclaringType.Properties.Add(propertyDefinition);
            this.propertySiteProcessor.Setters[fd] = set;

            if (syncVar.IsWrapped)
            {
                this.propertySiteProcessor.Getters[fd] = get;
            }
        }

        private MethodDefinition GenerateSyncVarGetter(FoundSyncVar syncVar)
        {
            var fd = syncVar.FieldDefinition;
            var originalType = syncVar.OriginalType;
            var originalName = syncVar.OriginalName;

            //Create the get method
            var get = fd.DeclaringType.AddMethod(
                    "get_Network" + originalName, MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    originalType);

            var worker = get.Body.GetILProcessor();
            this.WriteLoadField(worker, syncVar);

            worker.Append(worker.Create(OpCodes.Ret));

            get.SemanticsAttributes = MethodSemanticsAttributes.Getter;

            return get;
        }

        private MethodDefinition GenerateSyncVarSetterInitialOnly(FoundSyncVar syncVar)
        {
            // todo reduce duplicate code with this and GenerateSyncVarSetter
            var fd = syncVar.FieldDefinition;
            var originalType = syncVar.OriginalType;
            var originalName = syncVar.OriginalName;

            //Create the set method
            var set = fd.DeclaringType.AddMethod("set_Network" + originalName, MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig);
            var valueParam = set.AddParam(originalType, "value");
            set.SemanticsAttributes = MethodSemanticsAttributes.Setter;

            var worker = set.Body.GetILProcessor();
            this.WriteStoreField(worker, valueParam, syncVar);
            worker.Append(worker.Create(OpCodes.Ret));

            return set;
        }

        private MethodDefinition GenerateSyncVarSetter(FoundSyncVar syncVar)
        {
            var fd = syncVar.FieldDefinition;
            var originalType = syncVar.OriginalType;
            var originalName = syncVar.OriginalName;

            //Create the set method
            var set = fd.DeclaringType.AddMethod("set_Network" + originalName, MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig);
            var valueParam = set.AddParam(originalType, "value");
            set.SemanticsAttributes = MethodSemanticsAttributes.Setter;

            var worker = set.Body.GetILProcessor();

            // if (!SyncVarEqual(value, ref playerData))
            var endOfMethod = worker.Create(OpCodes.Nop);

            // this
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            // new value to set
            worker.Append(worker.Create(OpCodes.Ldarg, valueParam));
            // reference to field to set
            // make generic version of SetSyncVar with field type
            this.WriteLoadField(worker, syncVar);

            var syncVarEqual = this.module.ImportReference<NetworkBehaviour>(nb => nb.SyncVarEqual<object>(default, default));
            var syncVarEqualGm = new GenericInstanceMethod(syncVarEqual.GetElementMethod());
            syncVarEqualGm.GenericArguments.Add(originalType);
            worker.Append(worker.Create(OpCodes.Call, syncVarEqualGm));

            worker.Append(worker.Create(OpCodes.Brtrue, endOfMethod));

            // T oldValue = value
            var oldValue = set.AddLocal(originalType);
            this.WriteLoadField(worker, syncVar);
            worker.Append(worker.Create(OpCodes.Stloc, oldValue));

            // fieldValue = value
            this.WriteStoreField(worker, valueParam, syncVar);

            // this.SetDirtyBit(dirtyBit)
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldc_I8, syncVar.DirtyBit));
            worker.Append(worker.Create<NetworkBehaviour>(OpCodes.Call, nb => nb.SetDirtyBit(default)));

            if (syncVar.HasHook)
            {
                //if (base.isLocalClient && !getSyncVarHookGuard(dirtyBit))
                var label = worker.Create(OpCodes.Nop);
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                // if invokeOnServer, then `IsServer` will also cover the Host case too so we dont need to use an OR here
                if (syncVar.InvokeHookOnServer)
                    worker.Append(worker.Create(OpCodes.Call, (NetworkBehaviour nb) => nb.IsServer));
                else
                    worker.Append(worker.Create(OpCodes.Call, (NetworkBehaviour nb) => nb.IsLocalClient));

                worker.Append(worker.Create(OpCodes.Brfalse, label));
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldc_I8, syncVar.DirtyBit));
                worker.Append(worker.Create<NetworkBehaviour>(OpCodes.Call, nb => nb.GetSyncVarHookGuard(default)));
                worker.Append(worker.Create(OpCodes.Brtrue, label));

                // setSyncVarHookGuard(dirtyBit, true)
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldc_I8, syncVar.DirtyBit));
                worker.Append(worker.Create(OpCodes.Ldc_I4_1));
                worker.Append(worker.Create<NetworkBehaviour>(OpCodes.Call,
                    nb => nb.SetSyncVarHookGuard(default, default)));

                // call hook (oldValue, newValue)
                // Generates: OnValueChanged(oldValue, value)
                this.WriteCallHookMethodUsingArgument(worker, syncVar.Hook, oldValue);

                // setSyncVarHookGuard(dirtyBit, false)
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldc_I8, syncVar.DirtyBit));
                worker.Append(worker.Create(OpCodes.Ldc_I4_0));
                worker.Append(worker.Create<NetworkBehaviour>(OpCodes.Call, nb => nb.SetSyncVarHookGuard(default, default)));

                worker.Append(label);
            }

            worker.Append(endOfMethod);

            worker.Append(worker.Create(OpCodes.Ret));

            return set;
        }

        /// <summary>
        /// Writes Load field to IL worker, eg `this.field`
        /// <para>If syncvar is wrapped will use get_Value method instead</para>
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="syncVar"></param>
        private void WriteLoadField(ILProcessor worker, FoundSyncVar syncVar)
        {
            var fd = syncVar.FieldDefinition;
            var originalType = syncVar.OriginalType;

            worker.Append(worker.Create(OpCodes.Ldarg_0));

            if (syncVar.IsWrapped)
            {
                worker.Append(worker.Create(OpCodes.Ldflda, fd.MakeHostGenericIfNeeded()));
                var getter = this.module.ImportReference(fd.FieldType.Resolve().GetMethod("get_Value"));
                worker.Append(worker.Create(OpCodes.Call, getter));

                // When we use NetworkBehaviors, we normally use a derived class,
                // but the NetworkBehaviorSyncVar returns just NetworkBehavior
                // thus we need to cast it to the user specicfied type
                // otherwise IL2PP fails to build.  see #629
                if (getter.ReturnType.FullName != originalType.FullName)
                {
                    worker.Append(worker.Create(OpCodes.Castclass, originalType));
                }
            }
            else
            {
                worker.Append(worker.Create(OpCodes.Ldfld, fd.MakeHostGenericIfNeeded()));
            }
        }

        /// <summary>
        /// Writes Store field to IL worker, eg `this.field = `
        /// <para>If syncvar is wrapped will use set_Value method instead</para>
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="valueParam"></param>
        /// <param name="syncVar"></param>
        private void WriteStoreField(ILProcessor worker, ParameterDefinition valueParam, FoundSyncVar syncVar)
        {
            var fd = syncVar.FieldDefinition;

            if (syncVar.IsWrapped)
            {
                // there is a wrapper struct, call the setter
                var setter = this.module.ImportReference(fd.FieldType.Resolve().GetMethod("set_Value"));

                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldflda, fd.MakeHostGenericIfNeeded()));
                worker.Append(worker.Create(OpCodes.Ldarg, valueParam));
                worker.Append(worker.Create(OpCodes.Call, setter));
            }
            else
            {
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldarg, valueParam));
                worker.Append(worker.Create(OpCodes.Stfld, fd.MakeHostGenericIfNeeded()));
            }
        }

        private void WriteCallHookMethodUsingArgument(ILProcessor worker, SyncVarHook hook, VariableDefinition oldValue)
        {
            this.WriteCallHook(worker, hook, oldValue, null);
        }

        private void WriteCallHookMethodUsingField(ILProcessor worker, SyncVarHook hook, VariableDefinition oldValue, FoundSyncVar syncVarField)
        {
            if (syncVarField == null)
            {
                throw new ArgumentNullException(nameof(syncVarField));
            }

            this.WriteCallHook(worker, hook, oldValue, syncVarField);
        }

        private void WriteCallHook(ILProcessor worker, SyncVarHook hook, VariableDefinition oldValue, FoundSyncVar syncVarField)
        {
            if (hook.Method != null)
                this.WriteCallHookMethod(worker, hook.Method, hook.hookType, oldValue, syncVarField);
            if (hook.Event != null)
                this.WriteCallHookEvent(worker, hook.Event, hook.hookType, oldValue, syncVarField);
        }

        private void WriteCallHookMethod(ILProcessor worker, MethodDefinition hookMethod, SyncHookType hookType, VariableDefinition oldValue, FoundSyncVar syncVarField)
        {
            if (hookType != SyncHookType.MethodWith1Arg && hookType != SyncHookType.MethodWith2Arg)
                throw new ArgumentException($"hook type should be method, but was {hookType}", nameof(hookType));

            WriteStartFunctionCall();

            // write args
            if (hookType == SyncHookType.MethodWith2Arg)
                WriteOldValue();
            WriteNewValue();

            WriteEndFunctionCall();


            // *** Local functions used to write OpCodes ***
            // Local functions have access to function variables, no need to pass in args

            void WriteOldValue()
            {
                worker.Append(worker.Create(OpCodes.Ldloc, oldValue));
            }

            void WriteNewValue()
            {
                // write arg1 or this.field
                if (syncVarField == null)
                {
                    worker.Append(worker.Create(OpCodes.Ldarg_1));
                }
                else
                {
                    this.WriteLoadField(worker, syncVarField);
                }
            }

            // Writes this before method if it is not static
            void WriteStartFunctionCall()
            {
                // dont add this (Ldarg_0) if method is static
                if (!hookMethod.IsStatic)
                {
                    // this before method call
                    // eg this.onValueChanged
                    worker.Append(worker.Create(OpCodes.Ldarg_0));
                }
            }

            // Calls method
            void WriteEndFunctionCall()
            {
                // only use Callvirt when not static
                var OpCall = hookMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
                MethodReference hookMethodReference = hookMethod;

                if (hookMethodReference.DeclaringType.HasGenericParameters)
                {
                    // we need to get the Type<T>.HookMethod so convert it to a generic<T>.
                    var genericType = (GenericInstanceType)hookMethod.DeclaringType.ConvertToGenericIfNeeded();
                    hookMethodReference = hookMethod.MakeHostInstanceGeneric(genericType);
                }

                worker.Append(worker.Create(OpCall, this.module.ImportReference(hookMethodReference)));
            }
        }

        private void WriteCallHookEvent(ILProcessor worker, EventDefinition @event, SyncHookType hookType, VariableDefinition oldValue, FoundSyncVar syncVarField)
        {
            if (hookType != SyncHookType.EventWith1Arg && hookType != SyncHookType.EventWith2Arg)
                throw new ArgumentException($"hook type should be event, but was {hookType}", nameof(hookType));

            // get backing field for event, and sure it is generic instance (eg MyType<T>.myEvent
            var eventField = @event.DeclaringType.GetField(@event.Name).MakeHostGenericIfNeeded();

            // get action type with number of args
            var actionType = hookType == SyncHookType.EventWith1Arg
                ? typeof(Action<>)
                : typeof(Action<,>);

            // get Invoke method and make it correct type
            var invokeNonGeneric = this.module.ImportReference(actionType.GetMethod("Invoke"));
            var invoke = invokeNonGeneric.MakeHostInstanceGeneric((GenericInstanceType)@event.EventType);

            var nopEvent = worker.Create(OpCodes.Nop);
            var nopEnd = worker.Create(OpCodes.Nop);

            // **null check**
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldfld, eventField));
            // dup so we dont need to load field twice
            worker.Append(worker.Create(OpCodes.Dup));

            // jump to nop if null
            worker.Append(worker.Create(OpCodes.Brtrue, nopEvent));
            // pop because we didn't use field on if it was null
            worker.Append(worker.Create(OpCodes.Pop));
            worker.Append(worker.Create(OpCodes.Br, nopEnd));

            // **call invoke**
            worker.Append(nopEvent);

            if (hookType == SyncHookType.EventWith2Arg)
                WriteOldValue();
            WriteNewValue();

            worker.Append(worker.Create(OpCodes.Call, invoke));

            // after if (event!=null)
            worker.Append(nopEnd);


            // *** Local functions used to write OpCodes ***
            // Local functions have access to function variables, no need to pass in args

            void WriteOldValue()
            {
                worker.Append(worker.Create(OpCodes.Ldloc, oldValue));
            }

            void WriteNewValue()
            {
                // write arg1 or this.field
                if (syncVarField == null)
                {
                    worker.Append(worker.Create(OpCodes.Ldarg_1));
                }
                else
                {
                    this.WriteLoadField(worker, syncVarField);
                }
            }
        }

        private void GenerateSerialization()
        {
            Weaver.DebugLog(this.behaviour.TypeDefinition, "  GenerateSerialization");

            // Dont create method if users has manually overridden it
            if (this.behaviour.HasManualSerializeOverride())
                return;

            // dont create if there are no syncvars
            if (this.behaviour.SyncVars.Count == 0)
                return;

            var helper = new SerializeHelper(this.module, this.behaviour);
            var worker = helper.AddMethod();

            helper.AddLocals();
            helper.WriteBaseCall();

            helper.WriteIfInitial(() =>
            {
                foreach (var syncVar in this.behaviour.SyncVars)
                {
                    this.WriteFromField(worker, helper.WriterParameter, syncVar);
                }
            });

            // write dirty bits before the data fields
            helper.WriteDirtyBitMask();

            // generate a writer call for any dirty variable in this class

            // start at number of syncvars in parent
            foreach (var syncVar in this.behaviour.SyncVars)
            {
                // dont need to write field here if syncvar is InitialOnly
                if (syncVar.InitialOnly) { continue; }

                helper.WriteIfSyncVarDirty(syncVar, () =>
                {
                    // Generates a call to the writer for that field
                    this.WriteFromField(worker, helper.WriterParameter, syncVar);
                });
            }

            // generate: return dirtyLocal
            helper.WriteReturnDirty();
        }

        private void WriteFromField(ILProcessor worker, ParameterDefinition writerParameter, FoundSyncVar syncVar)
        {
            if (!syncVar.HasProcessed) return;

            var fieldRef = syncVar.FieldDefinition.MakeHostGenericIfNeeded();
            syncVar.ValueSerializer.AppendWriteField(this.module, worker, writerParameter, null, fieldRef);
        }

        private void GenerateDeserialization()
        {
            Weaver.DebugLog(this.behaviour.TypeDefinition, "  GenerateDeSerialization");

            // Dont create method if users has manually overridden it
            if (this.behaviour.HasManualDeserializeOverride())
                return;

            // dont create if there are no syncvars
            if (this.behaviour.SyncVars.Count == 0)
                return;


            var helper = new DeserializeHelper(this.module, this.behaviour);
            var worker = helper.AddMethod();

            helper.AddLocals();
            helper.WriteBaseCall();

            helper.WriteIfInitial(() =>
            {
                // For ititial spawn READ all values first, then invoke any hooks
                var oldValues = new VariableDefinition[this.behaviour.SyncVars.Count];
                for (var i = 0; i < this.behaviour.SyncVars.Count; i++)
                {
                    var syncVar = this.behaviour.SyncVars[i];
                    // StartHook create old value local variable,
                    oldValues[i] = this.StartHook(worker, helper.Method, syncVar, syncVar.OriginalType);
                    this.ReadToField(worker, helper.ReaderParameter, syncVar);
                }
                for (var i = 0; i < this.behaviour.SyncVars.Count; i++)
                {
                    var syncVar = this.behaviour.SyncVars[i];
                    this.EndHook(worker, syncVar, syncVar.OriginalType, oldValues[i]);
                }
            });

            helper.ReadDirtyBitMask();

            // conditionally read each syncvar
            foreach (var syncVar in this.behaviour.SyncVars)
            {
                // dont need to write field here if syncvar is InitialOnly
                if (syncVar.InitialOnly) { continue; }

                helper.WriteIfSyncVarDirty(syncVar, () =>
                {
                    var oldValue = this.StartHook(worker, helper.Method, syncVar, syncVar.OriginalType);
                    // read value and store in syncvar BEFORE calling the hook
                    this.ReadToField(worker, helper.ReaderParameter, syncVar);
                    this.EndHook(worker, syncVar, syncVar.OriginalType, oldValue);
                });
            }

            worker.Append(worker.Create(OpCodes.Ret));
        }

        /// <summary>
        /// If syncvar has a hook method, this will create a local variable with the old value of the field
        /// <para>should be called before storing the new value in the field</para>
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="deserialize"></param>
        /// <param name="syncVar"></param>
        /// <param name="originalType"></param>
        /// <returns></returns>
        private VariableDefinition StartHook(ILProcessor worker, MethodDefinition deserialize, FoundSyncVar syncVar, TypeReference originalType)
        {
            /*
             Generates code like:
                // for hook
                int oldValue = a
                Networka = reader.ReadPackedInt32()
                if (!SyncVarEqual(oldValue, ref a))
                    OnSetA(oldValue, Networka)
             */

            // Store old value in local variable, we need it for Hook
            // T oldValue = value
            VariableDefinition oldValue = null;
            if (syncVar.HasHook)
            {
                oldValue = deserialize.AddLocal(originalType);
                this.WriteLoadField(worker, syncVar);

                worker.Append(worker.Create(OpCodes.Stloc, oldValue));
            }

            return oldValue;
        }

        /// <summary>
        /// If syncvar has a hook method, this will invoke the hook method if it is changed with the old and new values
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="syncVar"></param>
        /// <param name="originalType"></param>
        /// <param name="oldValue"></param>
        private void EndHook(ILProcessor worker, FoundSyncVar syncVar, TypeReference originalType, VariableDefinition oldValue)
        {
            if (syncVar.HasHook)
            {
                // call hook
                // but only if SyncVar changed. otherwise a client would
                // get hook calls for all initial values, even if they
                // didn't change from the default values on the client.

                // Generates: if (!SyncVarEqual)
                var syncVarEqualLabel = worker.Create(OpCodes.Nop);

                // 'this.' for 'this.SyncVarEqual'
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                // 'oldValue'
                worker.Append(worker.Create(OpCodes.Ldloc, oldValue));
                // 'newValue'
                this.WriteLoadField(worker, syncVar);
                // call the function
                var syncVarEqual = this.module.ImportReference<NetworkBehaviour>(nb => nb.SyncVarEqual<object>(default, default));
                var syncVarEqualGm = new GenericInstanceMethod(syncVarEqual.GetElementMethod());
                syncVarEqualGm.GenericArguments.Add(originalType);
                worker.Append(worker.Create(OpCodes.Call, syncVarEqualGm));
                worker.Append(worker.Create(OpCodes.Brtrue, syncVarEqualLabel));

                // call the hook
                // Generates: OnValueChanged(oldValue, this.syncVar)
                this.WriteCallHookMethodUsingField(worker, syncVar.Hook, oldValue, syncVar);

                // Generates: end if (!SyncVarEqual)
                worker.Append(syncVarEqualLabel);
            }

        }

        private void ReadToField(ILProcessor worker, ParameterDefinition readerParameter, FoundSyncVar syncVar)
        {
            if (!syncVar.HasProcessed) return;

            // load this
            // read value
            // store to field

            worker.Append(worker.Create(OpCodes.Ldarg_0));

            syncVar.ValueSerializer.AppendRead(this.module, worker, readerParameter, syncVar.FieldDefinition.FieldType);

            worker.Append(worker.Create(OpCodes.Stfld, syncVar.FieldDefinition.MakeHostGenericIfNeeded()));
        }
    }
}

using System.Collections.Generic;
using Mirage.Weaver.NetworkBehaviours;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Mirage.Weaver
{
    public enum RemoteCallType
    {
        ServerRpc,
        ClientRpc
    }

    /// <summary>
    /// processes SyncVars, Cmds, Rpcs, etc. of NetworkBehaviours
    /// </summary>
    internal class NetworkBehaviourProcessor
    {
        private readonly TypeDefinition netBehaviourSubclass;
        private readonly IWeaverLogger logger;
        private readonly ServerRpcProcessor serverRpcProcessor;
        private readonly ClientRpcProcessor clientRpcProcessor;
        private readonly SyncVarProcessor syncVarProcessor;
        private readonly SyncObjectProcessor syncObjectProcessor;
        private readonly ConstFieldTracker rpcCounter;

        public NetworkBehaviourProcessor(TypeDefinition td, Readers readers, Writers writers, PropertySiteProcessor propertySiteProcessor, IWeaverLogger logger)
        {
            Weaver.DebugLog(td, "NetworkBehaviourProcessor");
            this.netBehaviourSubclass = td;
            this.logger = logger;
            this.serverRpcProcessor = new ServerRpcProcessor(this.netBehaviourSubclass.Module, readers, writers, logger);
            this.clientRpcProcessor = new ClientRpcProcessor(this.netBehaviourSubclass.Module, readers, writers, logger);
            this.syncVarProcessor = new SyncVarProcessor(this.netBehaviourSubclass.Module, readers, writers, propertySiteProcessor);
            this.syncObjectProcessor = new SyncObjectProcessor(readers, writers, logger);

            // no max for rpcs, index is sent as var int, so more rpc just means bigger header size (still smaller than 4 byte hash)
            this.rpcCounter = new ConstFieldTracker("RPC_COUNT", td, int.MaxValue, "Rpc");
        }

        // return true if modified
        public bool Process()
        {
            // only process once
            if (WasProcessed(this.netBehaviourSubclass))
            {
                return false;
            }
            Weaver.DebugLog(this.netBehaviourSubclass, $"Found NetworkBehaviour {this.netBehaviourSubclass.FullName}");

            Weaver.DebugLog(this.netBehaviourSubclass, "Process Start");
            MarkAsProcessed(this.netBehaviourSubclass);

            try
            {
                this.syncVarProcessor.ProcessSyncVars(this.netBehaviourSubclass, this.logger);
            }
            catch (NetworkBehaviourException e)
            {
                this.logger.Error(e);
            }

            this.syncObjectProcessor.ProcessSyncObjects(this.netBehaviourSubclass);

            this.ProcessRpcs();

            Weaver.DebugLog(this.netBehaviourSubclass, "Process Done");
            return true;
        }

        #region mark / check type as processed
        public const string ProcessedFunctionName = "MirageProcessed";

        // by adding an empty MirageProcessed() function
        public static bool WasProcessed(TypeDefinition td)
        {
            return td.GetMethod(ProcessedFunctionName) != null;
        }

        public static void MarkAsProcessed(TypeDefinition td)
        {
            if (!WasProcessed(td))
            {
                var versionMethod = td.AddMethod(ProcessedFunctionName, MethodAttributes.Private);
                var worker = versionMethod.Body.GetILProcessor();
                worker.Append(worker.Create(OpCodes.Ret));
            }
        }
        #endregion

        private void RegisterRpcs(List<RpcMethod> rpcs)
        {
            this.SetRpcCount(rpcs.Count);
            Weaver.DebugLog(this.netBehaviourSubclass, "  GenerateConstants ");

            this.netBehaviourSubclass.AddToConstructor(this.logger, (worker) =>
            {
                RegisterRpc.RegisterAll(worker, rpcs);
            });
        }

        private void SetRpcCount(int count)
        {
            // set const so that child classes know count of base classes
            this.rpcCounter.Set(count);

            // override virtual method so returns total
            var method = this.netBehaviourSubclass.AddMethod(nameof(NetworkBehaviour.GetRpcCount), MethodAttributes.Virtual | MethodAttributes.Family, typeof(int));
            var worker = method.Body.GetILProcessor();
            // write count of base+current so that `GetInBase` call will return total
            worker.Emit(OpCodes.Ldc_I4, this.rpcCounter.GetInBase() + count);
            worker.Emit(OpCodes.Ret);
        }

        private void ProcessRpcs()
        {
            // copy the list of methods because we will be adding methods in the loop
            var methods = new List<MethodDefinition>(this.netBehaviourSubclass.Methods);

            var rpcs = new List<RpcMethod>();

            var index = this.rpcCounter.GetInBase();
            foreach (var md in methods)
            {
                try
                {
                    var rpc = this.CheckAndProcessRpc(md, index);
                    if (rpc != null)
                    {
                        // increment only if rpc was count
                        index++;
                        rpcs.Add(rpc);
                    }
                }
                catch (RpcException e)
                {
                    this.logger.Error(e);
                }
            }

            this.RegisterRpcs(rpcs);
        }

        private RpcMethod CheckAndProcessRpc(MethodDefinition md, int index)
        {
            if (md.TryGetCustomAttribute<ServerRpcAttribute>(out var serverAttribute))
            {
                if (md.HasCustomAttribute<ClientRpcAttribute>()) throw new RpcException("Method should not have both ServerRpc and ClientRpc", md);

                return this.serverRpcProcessor.ProcessRpc(md, serverAttribute, index);
            }
            else if (md.TryGetCustomAttribute<ClientRpcAttribute>(out var clientAttribute))
            {
                return this.clientRpcProcessor.ProcessRpc(md, clientAttribute, index);
            }
            return null;
        }
    }
}

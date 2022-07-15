using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.Weaver
{
    /// <summary>
    /// Processes methods and fields to check their attrbiutes to make sure they are allowed on the type
    /// <para>
    /// Injects server/client active checks for [Server/Client] attributes
    /// </para>
    /// </summary>
    internal class AttributeProcessor
    {
        private readonly IWeaverLogger logger;
        private readonly MethodReference IsServer;
        private readonly MethodReference IsClient;
        private readonly MethodReference HasAuthority;
        private readonly MethodReference IsLocalPlayer;
        private bool modified = false;

        public AttributeProcessor(ModuleDefinition module, IWeaverLogger logger)
        {
            this.logger = logger;

            // Cache these so that we dont import them for each site we process
            this.IsServer = module.ImportReference((NetworkBehaviour nb) => nb.IsServer);
            this.IsClient = module.ImportReference((NetworkBehaviour nb) => nb.IsClient);
            this.HasAuthority = module.ImportReference((NetworkBehaviour nb) => nb.HasAuthority);
            this.IsLocalPlayer = module.ImportReference((NetworkBehaviour nb) => nb.IsLocalPlayer);
        }

        public bool ProcessTypes(IReadOnlyList<FoundType> foundTypes)
        {
            foreach (var foundType in foundTypes)
            {
                this.ProcessType(foundType);
            }

            return this.modified;
        }

        private void ProcessType(FoundType foundType)
        {
            foreach (var md in foundType.TypeDefinition.Methods)
            {
                this.ProcessMethod(md, foundType);
            }

            if (!foundType.IsNetworkBehaviour)
            {
                foreach (var fd in foundType.TypeDefinition.Fields)
                {
                    this.ProcessFields(fd, foundType);
                }
            }
        }

        /// <summary>
        /// Check if Syncvar or SyncObject are used outside of NetworkBehaviour
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="foundType"></param>
        private void ProcessFields(FieldDefinition fd, FoundType foundType)
        {
            if (fd.HasCustomAttribute<SyncVarAttribute>())
                this.logger.Error($"SyncVar {fd.Name} must be inside a NetworkBehaviour. {foundType.TypeDefinition.Name} is not a NetworkBehaviour", fd);

            // only check SyncObjects inside Monobehaviours
            if (foundType.IsMonoBehaviour && SyncObjectProcessor.ImplementsSyncObject(fd.FieldType))
            {
                this.logger.Error($"{fd.Name} is a SyncObject and can not be used inside Monobehaviour. {foundType.TypeDefinition.Name} is not a NetworkBehaviour", fd);
            }
        }

        private void ProcessMethod(MethodDefinition md, FoundType foundType)
        {
            if (IgnoreMethod(md))
                return;

            this.ProcessMethodAttributes(md, foundType);
        }

        /// <summary>
        /// Ignore if it is static constructor, or a Weaver Generated function
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        private static bool IgnoreMethod(MethodDefinition md)
        {
            return md.Name == ".cctor" ||
                md.Name == NetworkBehaviourProcessor.ProcessedFunctionName;
        }

        private void ProcessMethodAttributes(MethodDefinition md, FoundType foundType)
        {
            this.InjectGuard<ServerAttribute>(md, foundType, this.IsServer, "[Server] function '{0}' called when server not active");
            this.InjectGuard<ClientAttribute>(md, foundType, this.IsClient, "[Client] function '{0}' called when client not active");
            this.InjectGuard<HasAuthorityAttribute>(md, foundType, this.HasAuthority, "[Has Authority] function '{0}' called on player without authority");
            this.InjectGuard<LocalPlayerAttribute>(md, foundType, this.IsLocalPlayer, "[Local Player] function '{0}' called on nonlocal player");
            this.CheckAttribute<ServerRpcAttribute>(md, foundType);
            this.CheckAttribute<ClientRpcAttribute>(md, foundType);
        }

        private void CheckAttribute<TAttribute>(MethodDefinition md, FoundType foundType)
        {
            var attribute = md.GetCustomAttribute<TAttribute>();
            if (attribute == null)
                return;

            if (!foundType.IsNetworkBehaviour)
            {
                this.logger.Error($"{attribute.AttributeType.Name} method {md.Name} must be declared in a NetworkBehaviour", md);
            }
        }

        private void InjectGuard<TAttribute>(MethodDefinition md, FoundType foundType, MethodReference predicate, string format)
        {
            var attribute = md.GetCustomAttribute<TAttribute>();
            if (attribute == null)
                return;

            if (md.IsAbstract)
            {
                this.logger.Error($"{typeof(TAttribute)} can't be applied to abstract method. Apply to override methods instead.", md);
                return;
            }

            if (!foundType.IsNetworkBehaviour)
            {
                this.logger.Error($"{attribute.AttributeType.Name} method {md.Name} must be declared in a NetworkBehaviour", md);
                return;
            }

            if (md.Name == "Awake" && !md.HasParameters)
            {
                this.logger.Error($"{attribute.AttributeType.Name} will not work on the Awake method.", md);
                return;
            }

            // dont need to set modified for errors, so we set it here when we start doing ILProcessing
            this.modified = true;

            var throwError = attribute.GetField("error", true);
            var worker = md.Body.GetILProcessor();
            var top = md.Body.Instructions[0];

            worker.InsertBefore(top, worker.Create(OpCodes.Ldarg_0));
            worker.InsertBefore(top, worker.Create(OpCodes.Call, predicate));
            worker.InsertBefore(top, worker.Create(OpCodes.Brtrue, top));
            if (throwError)
            {
                var message = string.Format(format, md.Name);
                worker.InsertBefore(top, worker.Create(OpCodes.Ldstr, message));
                worker.InsertBefore(top, worker.Create(OpCodes.Newobj, () => new MethodInvocationException("")));
                worker.InsertBefore(top, worker.Create(OpCodes.Throw));
            }
            InjectGuardParameters(md, worker, top);
            InjectGuardReturnValue(md, worker, top);
            worker.InsertBefore(top, worker.Create(OpCodes.Ret));
        }

        // this is required to early-out from a function with "ref" or "out" parameters
        private static void InjectGuardParameters(MethodDefinition md, ILProcessor worker, Instruction top)
        {
            var offset = md.Resolve().IsStatic ? 0 : 1;
            for (var index = 0; index < md.Parameters.Count; index++)
            {
                var param = md.Parameters[index];
                if (param.IsOut)
                {
                    var elementType = param.ParameterType.GetElementType();

                    var elementLocal = md.AddLocal(elementType);

                    worker.InsertBefore(top, worker.Create(OpCodes.Ldarg, index + offset));
                    worker.InsertBefore(top, worker.Create(OpCodes.Ldloca, elementLocal));
                    worker.InsertBefore(top, worker.Create(OpCodes.Initobj, elementType));
                    worker.InsertBefore(top, worker.Create(OpCodes.Ldloc, elementLocal));
                    worker.InsertBefore(top, worker.Create(OpCodes.Stobj, elementType));
                }
            }
        }

        // this is required to early-out from a function with a return value.
        private static void InjectGuardReturnValue(MethodDefinition md, ILProcessor worker, Instruction top)
        {
            if (!md.ReturnType.Is(typeof(void)))
            {
                var returnLocal = md.AddLocal(md.ReturnType);
                worker.InsertBefore(top, worker.Create(OpCodes.Ldloca, returnLocal));
                worker.InsertBefore(top, worker.Create(OpCodes.Initobj, md.ReturnType));
                worker.InsertBefore(top, worker.Create(OpCodes.Ldloc, returnLocal));
            }
        }
    }
}

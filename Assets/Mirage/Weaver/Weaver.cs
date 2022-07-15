using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace Mirage.Weaver
{
    /// <summary>
    /// Weaves an Assembly
    /// <para>
    /// Debug Defines:<br />
    /// - <c>WEAVER_DEBUG_LOGS</c><br />
    /// - <c>WEAVER_DEBUG_TIMER</c><br />
    /// </para>
    /// </summary>
    public class Weaver
    {
        private readonly IWeaverLogger logger;
        private Readers readers;
        private Writers writers;
        private PropertySiteProcessor propertySiteProcessor;
        private WeaverDiagnosticsTimer timer;

        private AssemblyDefinition CurrentAssembly { get; set; }

        [Conditional("WEAVER_DEBUG_LOGS")]
        public static void DebugLog(TypeDefinition td, string message)
        {
            Console.WriteLine($"Weaver[{td.Name}]{message}");
        }

        public Weaver(IWeaverLogger logger)
        {
            this.logger = logger;
        }

        public AssemblyDefinition Weave(ICompiledAssembly compiledAssembly)
        {
            try
            {
                this.timer = new WeaverDiagnosticsTimer() { writeToFile = true };
                this.timer.Start(compiledAssembly.Name);

                using (this.timer.Sample("AssemblyDefinitionFor"))
                {
                    this.CurrentAssembly = AssemblyDefinitionFor(compiledAssembly);
                }

                var module = this.CurrentAssembly.MainModule;
                this.readers = new Readers(module, this.logger);
                this.writers = new Writers(module, this.logger);
                this.propertySiteProcessor = new PropertySiteProcessor();
                var rwProcessor = new ReaderWriterProcessor(module, this.readers, this.writers);

                var modified = false;
                using (this.timer.Sample("ReaderWriterProcessor"))
                {
                    modified = rwProcessor.Process();
                }

                var foundTypes = this.FindAllClasses(module);

                using (this.timer.Sample("AttributeProcessor"))
                {
                    var attributeProcessor = new AttributeProcessor(module, this.logger);
                    modified |= attributeProcessor.ProcessTypes(foundTypes);
                }

                using (this.timer.Sample("WeaveNetworkBehavior"))
                {
                    foreach (var foundType in foundTypes)
                    {
                        if (foundType.IsNetworkBehaviour)
                            modified |= this.WeaveNetworkBehavior(foundType);
                    }
                }


                if (modified)
                {
                    using (this.timer.Sample("propertySiteProcessor"))
                    {
                        this.propertySiteProcessor.Process(module);
                    }

                    using (this.timer.Sample("InitializeReaderAndWriters"))
                    {
                        rwProcessor.InitializeReaderAndWriters();
                    }
                }

                return this.CurrentAssembly;
            }
            catch (Exception e)
            {
                this.logger.Error("Exception :" + e);
                return null;
            }
            finally
            {
                // end in finally incase it return early
                this.timer?.End();
            }
        }

        public static AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly)
        {
            var assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);

            //apparently, it will happen that when we ask to resolve a type that lives inside MLAPI.Runtime, and we
            //are also postprocessing MLAPI.Runtime, type resolving will fail, because we do not actually try to resolve
            //inside the assembly we are processing. Let's make sure we do that, so that we can use postprocessor features inside
            //MLAPI.Runtime itself as well.
            assemblyResolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }

        private IReadOnlyList<FoundType> FindAllClasses(ModuleDefinition module)
        {
            using (this.timer.Sample("FindAllClasses"))
            {
                var foundTypes = new List<FoundType>();
                foreach (var type in module.Types)
                {
                    this.ProcessType(type, foundTypes);

                    foreach (var nested in type.NestedTypes)
                    {
                        this.ProcessType(nested, foundTypes);
                    }
                }

                return foundTypes;
            }
        }

        private void ProcessType(TypeDefinition type, List<FoundType> foundTypes)
        {
            if (!type.IsClass) return;

            var parent = type.BaseType;
            var isNetworkBehaviour = false;
            var isMonoBehaviour = false;
            while (parent != null)
            {
                if (parent.Is<NetworkBehaviour>())
                {
                    isNetworkBehaviour = true;
                    isMonoBehaviour = true;
                    break;
                }
                if (parent.Is<MonoBehaviour>())
                {
                    isMonoBehaviour = true;
                    break;
                }

                parent = parent.TryResolveParent();
            }

            foundTypes.Add(new FoundType(type, isNetworkBehaviour, isMonoBehaviour));
        }

        private bool WeaveNetworkBehavior(FoundType foundType)
        {
            var behaviourClasses = FindAllBaseTypes(foundType);

            var modified = false;
            // process this and base classes from parent to child order
            for (var i = behaviourClasses.Count - 1; i >= 0; i--)
            {
                var behaviour = behaviourClasses[i];
                if (NetworkBehaviourProcessor.WasProcessed(behaviour)) { continue; }

                modified |= new NetworkBehaviourProcessor(behaviour, this.readers, this.writers, this.propertySiteProcessor, this.logger).Process();
            }
            return modified;
        }

        /// <summary>
        /// Returns all base types that are between the type and NetworkBehaviour
        /// </summary>
        /// <param name="foundType"></param>
        /// <returns></returns>
        private static List<TypeDefinition> FindAllBaseTypes(FoundType foundType)
        {
            var behaviourClasses = new List<TypeDefinition>();

            var type = foundType.TypeDefinition;
            while (type != null)
            {
                if (type.Is<NetworkBehaviour>())
                {
                    break;
                }

                behaviourClasses.Add(type);
                type = type.BaseType.TryResolve();
            }

            return behaviourClasses;
        }
    }

    public class FoundType
    {
        public readonly TypeDefinition TypeDefinition;

        /// <summary>
        /// Is Derived From NetworkBehaviour
        /// </summary>
        public readonly bool IsNetworkBehaviour;

        public readonly bool IsMonoBehaviour;

        public FoundType(TypeDefinition typeDefinition, bool isNetworkBehaviour, bool isMonoBehaviour)
        {
            this.TypeDefinition = typeDefinition;
            this.IsNetworkBehaviour = isNetworkBehaviour;
            this.IsMonoBehaviour = isMonoBehaviour;
        }

        public override string ToString()
        {
            return this.TypeDefinition.ToString();
        }
    }
}

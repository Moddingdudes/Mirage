using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirage.Weaver;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor.Compilation;
using UnityEngine;

namespace Mirage.Tests.Weaver
{
    public class CompiledAssembly : ICompiledAssembly
    {
        private readonly string assemblyPath;
        private InMemoryAssembly inMemoryAssembly;

        public CompiledAssembly(string assemblyPath, AssemblyBuilder assemblyBuilder)
        {
            this.assemblyPath = assemblyPath;
            this.Defines = assemblyBuilder.defaultDefines;
            this.References = assemblyBuilder.defaultReferences;
        }

        public InMemoryAssembly InMemoryAssembly
        {
            get
            {

                if (this.inMemoryAssembly == null)
                {
                    var peData = File.ReadAllBytes(this.assemblyPath);

                    var pdbFileName = Path.GetFileNameWithoutExtension(this.assemblyPath) + ".pdb";

                    var pdbData = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(this.assemblyPath), pdbFileName));

                    this.inMemoryAssembly = new InMemoryAssembly(peData, pdbData);
                }
                return this.inMemoryAssembly;
            }
        }

        public string Name => Path.GetFileNameWithoutExtension(this.assemblyPath);

        public string[] References { get; set; }

        public string[] Defines { get; set; }
    }

    public class Assembler
    {
        public string OutputFile { get; set; }
        public string ProjectPathFile => Path.Combine(WeaverTestLocator.OutputDirectory, this.OutputFile);
        public List<CompilerMessage> CompilerMessages { get; private set; }
        public bool CompilerErrors { get; private set; }

        private readonly HashSet<string> sourceFiles = new HashSet<string>();

        public Assembler()
        {
            this.CompilerMessages = new List<CompilerMessage>();
        }

        // Add a range of source files to compile
        public void AddSourceFiles(string[] sourceFiles)
        {
            foreach (var src in sourceFiles)
            {
                this.sourceFiles.Add(Path.Combine(WeaverTestLocator.OutputDirectory, src));
            }
        }

        // Delete output dll / pdb / mdb
        public void DeleteOutput()
        {
            // "x.dll" shortest possible dll name
            if (this.OutputFile.Length < 5)
            {
                return;
            }

            try
            {
                File.Delete(this.ProjectPathFile);
            }
            catch { /* Do Nothing */ }

            try
            {
                File.Delete(Path.ChangeExtension(this.ProjectPathFile, ".pdb"));
            }
            catch { /* Do Nothing */ }

            try
            {
                File.Delete(Path.ChangeExtension(this.ProjectPathFile, ".dll.mdb"));
            }
            catch { /* Do Nothing */ }
        }

        /// <summary>
        /// Builds and Weaves an Assembly with references to unity engine and other asmdefs.
        /// <para>
        ///     NOTE: Does not write the weaved assemble to disk
        /// </para>
        /// </summary>
        public AssemblyDefinition Build(IWeaverLogger logger)
        {
            AssemblyDefinition assembly = null;

            // This will compile scripts with the same references as files in the asset folder.
            // This means that the dll will get references to all asmdef just as if it was the default "Assembly-CSharp.dll"
            var assemblyBuilder = new AssemblyBuilder(this.ProjectPathFile, this.sourceFiles.ToArray())
            {
                referencesOptions = ReferencesOptions.UseEngineModules
            };

            assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
            {
#if !UNITY_2020_2_OR_NEWER
                CompilerMessages.AddRange(compilerMessages);
                foreach (CompilerMessage cm in compilerMessages)
                {
                    if (cm.type == CompilerMessageType.Error)
                    {
                        Debug.LogErrorFormat("{0}:{1} -- {2}", cm.file, cm.line, cm.message);
                        CompilerErrors = true;
                    }
                }
#endif

                // assembly builder does not call ILPostProcessor (WTF Unity?),  so we must invoke it ourselves.
                var compiledAssembly = new CompiledAssembly(assemblyPath, assemblyBuilder);

                var weaver = new Mirage.Weaver.Weaver(logger);

                assembly = weaver.Weave(compiledAssembly);

                // NOTE: we need to write to check for ArgumentException from writing
                if (assembly != null)
                    WriteAssembly(assembly);
            };

            // Start build of assembly
            if (!assemblyBuilder.Build())
            {
                Debug.LogErrorFormat("Failed to start build of assembly {0}", assemblyBuilder.assemblyPath);
                return assembly;
            }

            while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
            {
                System.Threading.Thread.Sleep(10);
            }

            return assembly;
        }

        private static void WriteAssembly(AssemblyDefinition assembly)
        {
            var file = $"./temp/WeaverTests/{assembly.Name}.dll";
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            assembly.Write(file);
        }
    }
}

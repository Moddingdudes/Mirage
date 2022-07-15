using System;
using System.IO;
using System.Linq;
using Mirage.Logging;
using Mirage.Weaver;
using Mono.Cecil;
using NUnit.Framework;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace Mirage.Tests.Weaver
{
    public class AssertionMethodAttribute : Attribute { }

    public abstract class TestsBuildFromTestName : Tests
    {
        [SetUp]
        public virtual void TestSetup()
        {
            var className = TestContext.CurrentContext.Test.ClassName.Split('.').Last();

            this.BuildAndWeaveTestAssembly(className, TestContext.CurrentContext.Test.Name);
        }

        [AssertionMethod]
        protected void IsSuccess()
        {
            Assert.That(this.weaverLog.Diagnostics, Is.Empty, $"Failed because there are Diagnostics messages: \n  {string.Join("\n  ", this.weaverLog.Diagnostics.Select(x => x.MessageData))}\n");
        }

        /// <summary>
        /// Like <see cref="IsSuccess"/> but doesn't fail if there are warnings
        /// </summary>
        [AssertionMethod]
        protected void NoErrors()
        {
            var errors = this.weaverLog.Diagnostics.Where(x => x.DiagnosticType == DiagnosticType.Error).ToArray();
            Assert.That(errors, Is.Empty, $"Failed because there are Error messages: \n  {string.Join("\n  ", errors.Select(d => d.MessageData))}\n");
        }

        [AssertionMethod]
        protected void HasErrorCount(int count)
        {
            var errorMessages = this.weaverLog.Diagnostics
                .Where(d => d.DiagnosticType == DiagnosticType.Error)
                .Select(d => d.MessageData).ToArray();

            Assert.That(errorMessages.Length, Is.EqualTo(count), $"Error messages: \n  {string.Join("\n  ", errorMessages)}\n");
        }

        [AssertionMethod]
        protected void HasError(string messsage, string atType)
        {
            var fullMessage = $"{messsage} (at {atType})";
            var errorMessages = this.weaverLog.Diagnostics
                .Where(d => d.DiagnosticType == DiagnosticType.Error)
                .Select(d => d.MessageData).ToArray();

            Assert.That(errorMessages, Contains.Item(fullMessage),
                $"Could not find error message in list\n" +
                $"  Message: \n    {fullMessage}\n" +
                $"  Errors: \n    {string.Join("\n    ", errorMessages)}\n"
                );
        }

        [AssertionMethod]
        protected void HasWarning(string messsage, string atType)
        {
            var fullMessage = $"{messsage} (at {atType})";
            var warningMessages = this.weaverLog.Diagnostics
                .Where(d => d.DiagnosticType == DiagnosticType.Warning)
                .Select(d => d.MessageData).ToArray();

            Assert.That(warningMessages, Contains.Item(fullMessage),
                $"Could not find warning message in list\n" +
                $"  Message: \n    {fullMessage}\n" +
                $"  Warings: \n    {string.Join("\n    ", warningMessages)}\n"
                );
        }
    }

    [TestFixture]
    public abstract class Tests
    {
        public static readonly ILogger logger = LogFactory.GetLogger<Tests>(LogType.Exception);

        protected WeaverLogger weaverLog = new WeaverLogger();

        protected AssemblyDefinition assembly;

        protected Assembler assembler;

        protected void BuildAndWeaveTestAssembly(string className, string testName)
        {
            this.weaverLog.Diagnostics.Clear();
            this.assembler = new Assembler();

            var testSourceDirectory = className + "~";
            this.assembler.OutputFile = Path.Combine(testSourceDirectory, testName + ".dll");
            this.assembler.AddSourceFiles(new string[] { Path.Combine(testSourceDirectory, testName + ".cs") });
            this.assembly = this.assembler.Build(this.weaverLog);

            Assert.That(this.assembler.CompilerErrors, Is.False);
            foreach (var error in this.weaverLog.Diagnostics)
            {
                // ensure all errors have a location
                Assert.That(error.MessageData, Does.Match(@"\(at .*\)$"));
            }
        }

        [TearDown]
        public void TestCleanup()
        {
            this.assembler.DeleteOutput();
        }
    }
}

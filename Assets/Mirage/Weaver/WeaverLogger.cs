using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace Mirage.Weaver
{
    public class WeaverLogger : IWeaverLogger
    {
        public List<DiagnosticMessage> Diagnostics = new List<DiagnosticMessage>();


        public void Error(string message)
        {
            this.AddMessage(message, null, DiagnosticType.Error);
        }

        public void Error(string message, MemberReference mr)
        {
            this.Error($"{message} (at {mr})");
        }

        public void Error(string message, MemberReference mr, SequencePoint sequencePoint)
        {
            this.AddMessage($"{message} (at {mr})", sequencePoint, DiagnosticType.Error);
        }

        public void Error(string message, MethodDefinition md)
        {
            this.Error(message, md, md.DebugInformation.SequencePoints.FirstOrDefault());
        }


        public void Warning(string message)
        {
            this.AddMessage($"{message}", null, DiagnosticType.Warning);
        }

        public void Warning(string message, MemberReference mr)
        {
            this.Warning($"{message} (at {mr})");
        }

        public void Warning(string message, MemberReference mr, SequencePoint sequencePoint)
        {
            this.AddMessage($"{message} (at {mr})", sequencePoint, DiagnosticType.Warning);
        }

        public void Warning(string message, MethodDefinition md)
        {
            this.Warning(message, md, md.DebugInformation.SequencePoints.FirstOrDefault());
        }


        private void AddMessage(string message, SequencePoint sequencePoint, DiagnosticType diagnosticType)
        {
            this.Diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = diagnosticType,
                File = sequencePoint?.Document.Url.Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", ""),
                Line = sequencePoint?.StartLine ?? 0,
                Column = sequencePoint?.StartColumn ?? 0,
                MessageData = message
            });
        }
    }
}

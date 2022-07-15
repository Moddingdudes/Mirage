using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.Weaver.Serialization
{
    internal class PackerSerializer : ValueSerializer
    {
        private readonly FieldReference packerField;
        private readonly LambdaExpression packMethod;
        private readonly LambdaExpression unpackMethod;

        public override bool IsIntType { get; }

        public PackerSerializer(FieldDefinition packerField, LambdaExpression packMethod, LambdaExpression unpackMethod, bool isIntType)
        {
            this.packerField = packerField.MakeHostGenericIfNeeded();

            this.packMethod = packMethod;
            this.unpackMethod = unpackMethod;

            this.IsIntType = isIntType;
        }

        public override void AppendWriteField(ModuleDefinition module, ILProcessor worker, ParameterDefinition writerParameter, ParameterDefinition typeParameter, FieldReference fieldReference)
        {
            // if PackerField is null it means there was an error earlier, so we dont need to do anything here
            if (this.packerField == null) { return; }

            // Generates: packer.pack(writer, field)
            worker.Append(worker.Create(OpCodes.Ldsfld, this.packerField));
            worker.Append(LoadParamOrArg0(worker, writerParameter));
            worker.Append(LoadParamOrArg0(worker, typeParameter));
            worker.Append(worker.Create(OpCodes.Ldfld, fieldReference));
            worker.Append(worker.Create(OpCodes.Call, this.packMethod));
        }

        public override void AppendWriteParameter(ModuleDefinition module, ILProcessor worker, VariableDefinition writer, ParameterDefinition valueParameter)
        {
            worker.Append(worker.Create(OpCodes.Ldsfld, this.packerField));
            worker.Append(worker.Create(OpCodes.Ldloc, writer));
            worker.Append(worker.Create(OpCodes.Ldarg, valueParameter));
            worker.Append(worker.Create(OpCodes.Call, this.packMethod));
        }

        public override void AppendRead(ModuleDefinition module, ILProcessor worker, ParameterDefinition readerParameter, TypeReference fieldType)
        {
            // if PackerField is null it means there was an error earlier, so we dont need to do anything here
            if (this.packerField == null) { return; }

            // Generates: ... = packer.unpack(reader)
            worker.Append(worker.Create(OpCodes.Ldsfld, this.packerField));
            worker.Append(worker.Create(OpCodes.Ldarg, readerParameter));
            worker.Append(worker.Create(OpCodes.Call, this.unpackMethod));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.Weaver
{
    public abstract class SerializeFunctionBase
    {
        protected readonly Dictionary<TypeReference, MethodReference> funcs = new Dictionary<TypeReference, MethodReference>(new TypeReferenceComparer());
        private readonly IWeaverLogger logger;
        protected readonly ModuleDefinition module;

        public int Count => this.funcs.Count;

        /// <summary>
        /// Type used for logging, eg write or read
        /// </summary>
        protected abstract string FunctionTypeLog { get; }

        /// <summary>
        /// Name for const that will tell other asmdef's that type has already generated function
        /// </summary>
        protected abstract Type GeneratedAttribute { get; }

        protected SerializeFunctionBase(ModuleDefinition module, IWeaverLogger logger)
        {
            this.logger = logger;
            this.module = module;
        }

        public void Register(TypeReference dataType, MethodReference methodReference)
        {
            if (this.funcs.ContainsKey(dataType))
            {
                this.logger.Warning(
                    $"Registering a {this.FunctionTypeLog} for {dataType.FullName} when one already exists\n" +
                    $"  old:{this.funcs[dataType].FullName}\n" +
                    $"  new:{methodReference.FullName}",
                    methodReference.Resolve());
            }

            // we need to import type when we Initialize Writers so import here in case it is used anywhere else
            var imported = this.module.ImportReference(dataType);
            this.funcs[imported] = methodReference;

            // mark type as generated,
            this.MarkAsGenerated(dataType);
        }

        /// <summary>
        /// Marks type as having write/read function if it is in the current module
        /// </summary>
        private void MarkAsGenerated(TypeReference typeReference)
        {
            this.MarkAsGenerated(typeReference.Resolve());
        }

        /// <summary>
        /// Marks type as having write/read function if it is in the current module
        /// </summary>
        private void MarkAsGenerated(TypeDefinition typeDefinition)
        {
            // if in this module, then mark as generated
            if (typeDefinition.Module != this.module)
                return;

            // dont add twice
            if (typeDefinition.HasCustomAttribute(this.GeneratedAttribute))
                return;

            typeDefinition.AddCustomAttribute(this.module, this.GeneratedAttribute);
        }

        /// <summary>
        /// Check if type has a write/read function generated in another module
        /// <para>returns false if type is a member of current module</para>
        /// </summary>
        private bool HasGeneratedFunctionInAnotherModule(TypeReference typeReference)
        {
            var def = typeReference.Resolve();
            // if type is in this module, then we want to generate new function
            if (def.Module == this.module)
                return false;

            return def.HasCustomAttribute(this.GeneratedAttribute);
        }

        /// <summary>
        /// Trys to get writer for type, returns null if not found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequencePoint"></param>
        /// <returns>found methohd or null</returns>
        public MethodReference TryGetFunction<T>(SequencePoint sequencePoint) =>
            this.TryGetFunction(this.module.ImportReference<T>(), sequencePoint);

        /// <summary>
        /// Trys to get writer for type, returns null if not found
        /// </summary>
        /// <param name="typeReference"></param>
        /// <param name="sequencePoint"></param>
        /// <returns>found methohd or null</returns>
        public MethodReference TryGetFunction(TypeReference typeReference, SequencePoint sequencePoint)
        {
            try
            {
                return this.GetFunction_Thorws(typeReference);
            }
            catch (SerializeFunctionException e)
            {
                this.logger.Error(e, sequencePoint);
                return null;
            }
        }

        /// <summary>
        /// checks if function exists for type, if it does not exist it trys to generate it
        /// </summary>
        /// <param name="typeReference"></param>
        /// <param name="sequencePoint"></param>
        /// <returns></returns>
        /// <exception cref="SerializeFunctionException">Throws if unable to find or create function</exception>
        // todo rename this to GetFunction once other classes are able to catch Exception
        public MethodReference GetFunction_Thorws(TypeReference typeReference)
        {
            // if is <T> then  just return generic write./read with T as the generic argument
            if (typeReference.IsGenericParameter)
            {
                return this.CreateGenericFunction(typeReference);
            }

            // check if there is already a known function for type
            // this will find extention methods within this module
            if (this.funcs.TryGetValue(typeReference, out var foundFunc))
            {
                return foundFunc;
            }
            else
            {
                // before generating new function, check if one was generated for type in its own module
                if (this.HasGeneratedFunctionInAnotherModule(typeReference))
                {
                    return this.CreateGenericFunction(typeReference);
                }

                return this.GenerateFunction(this.module.ImportReference(typeReference));
            }
        }



        private MethodReference GenerateFunction(TypeReference typeReference)
        {
            if (typeReference.IsByReference)
            {
                throw new SerializeFunctionException($"Cannot pass {typeReference.Name} by reference", typeReference);
            }

            // Arrays are special, if we resolve them, we get the element type,
            // eg int[] resolves to int
            // therefore process this before checks below
            if (typeReference.IsArray)
            {
                if (typeReference.IsMultidimensionalArray())
                {
                    throw new SerializeFunctionException($"{typeReference.Name} is an unsupported type. Multidimensional arrays are not supported", typeReference);
                }
                var elementType = typeReference.GetElementType();
                return this.GenerateCollectionFunction(typeReference, elementType, this.ArrayExpression);
            }

            // check for collections
            if (typeReference.Is(typeof(Nullable<>)))
            {
                var genericInstance = (GenericInstanceType)typeReference;
                var elementType = genericInstance.GenericArguments[0];

                return this.GenerateCollectionFunction(typeReference, elementType, this.NullableExpression);
            }
            if (typeReference.Is(typeof(ArraySegment<>)))
            {
                var genericInstance = (GenericInstanceType)typeReference;
                var elementType = genericInstance.GenericArguments[0];

                return this.GenerateCollectionFunction(typeReference, elementType, this.SegmentExpression);
            }
            if (typeReference.Is(typeof(List<>)))
            {
                var genericInstance = (GenericInstanceType)typeReference;
                var elementType = genericInstance.GenericArguments[0];

                return this.GenerateCollectionFunction(typeReference, elementType, this.ListExpression);
            }


            // check for invalid types
            var typeDefinition = typeReference.Resolve();
            if (typeDefinition == null)
            {
                throw this.ThrowCantGenerate(typeReference);
            }

            if (typeDefinition.IsEnum)
            {
                // serialize enum as their base type
                return this.GenerateEnumFunction(typeReference);
            }

            if (typeDefinition.IsDerivedFrom<NetworkBehaviour>())
            {
                return this.GetNetworkBehaviourFunction(typeReference);
            }

            // unity base types are invalid
            if (typeDefinition.IsDerivedFrom<UnityEngine.Component>())
            {
                throw this.ThrowCantGenerate(typeReference, "component type");
            }
            if (typeReference.Is<UnityEngine.Object>())
            {
                throw this.ThrowCantGenerate(typeReference);
            }
            if (typeReference.Is<UnityEngine.ScriptableObject>())
            {
                throw this.ThrowCantGenerate(typeReference);
            }

            // if it is genericInstance, then we can generate writer for it
            if (!typeReference.IsGenericInstance && typeDefinition.HasGenericParameters)
            {
                throw this.ThrowCantGenerate(typeReference, "generic type");
            }
            if (typeDefinition.IsInterface)
            {
                throw this.ThrowCantGenerate(typeReference, "interface");
            }
            if (typeDefinition.IsAbstract)
            {
                throw this.ThrowCantGenerate(typeReference, "abstract class");
            }

            // generate writer for class/struct 
            var generated = this.GenerateClassOrStructFunction(typeReference);
            this.MarkAsGenerated(typeDefinition);

            return generated;
        }

        private SerializeFunctionException ThrowCantGenerate(TypeReference typeReference, string typeDescription = null)
        {
            var reasonStr = string.IsNullOrEmpty(typeDescription) ? string.Empty : $"{typeDescription} ";
            return new SerializeFunctionException($"Cannot generate {this.FunctionTypeLog} for {reasonStr}{typeReference.Name}. Use a supported type or provide a custom {this.FunctionTypeLog}", typeReference);
        }

        /// <summary>
        /// Creates Generic instance for Write{T} or Read{T} with <paramref name="argument"/> as then generic argument
        /// <para>Can also create Write{int} if real type is given instead of generic argument</para>
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        private GenericInstanceMethod CreateGenericFunction(TypeReference argument)
        {
            var method = this.GetGenericFunction();

            var generic = new GenericInstanceMethod(method);
            generic.GenericArguments.Add(argument);

            return generic;
        }

        /// <summary>
        /// Gets generic Write{T} or Read{T}
        /// </summary>
        /// <returns></returns>
        protected abstract MethodReference GetGenericFunction();

        protected abstract MethodReference GetNetworkBehaviourFunction(TypeReference typeReference);

        protected abstract MethodReference GenerateEnumFunction(TypeReference typeReference);
        protected abstract MethodReference GenerateCollectionFunction(TypeReference typeReference, TypeReference elementType, Expression<Action> genericExpression);

        protected abstract Expression<Action> ArrayExpression { get; }
        protected abstract Expression<Action> ListExpression { get; }
        protected abstract Expression<Action> SegmentExpression { get; }
        protected abstract Expression<Action> NullableExpression { get; }

        protected abstract MethodReference GenerateClassOrStructFunction(TypeReference typeReference);
    }
}

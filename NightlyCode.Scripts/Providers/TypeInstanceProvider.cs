using System;
using System.Linq;
using System.Reflection;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Parser.Resolvers;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Providers {

    /// <summary>
    /// provides instances of arbitrary types
    /// </summary>
    public class TypeInstanceProvider : ITypeInstanceProvider {
        readonly IMethodResolver resolver;
        readonly Type type;

        /// <summary>
        /// creates a new <see cref="TypeInstanceProvider"/>
        /// </summary>
        /// <param name="type">type to create</param>
        public TypeInstanceProvider(Type type, IMethodResolver resolver) {
            this.type = type;
            this.resolver = resolver;
        }

        /// <inheritdoc />
        public Type ProvidedType => type;

        /// <inheritdoc />
        public object Create(IScriptToken[] parameters, ScriptContext context) {
            object[] parametervalues = parameters.Select(p => p.Execute(context)).ToArray();
            ConstructorInfo constructor=resolver.ResolveConstructor(type, parametervalues);
            return constructor.Invoke(MethodOperations.CreateParameters(constructor.GetParameters(), parametervalues).ToArray());
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{type.Name}";
        }
    }
}
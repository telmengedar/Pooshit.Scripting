using System;
using System.Linq;
using System.Reflection;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Providers {

    /// <summary>
    /// provides instances of arbitrary types
    /// </summary>
    public class TypeInstanceProvider : ITypeInstanceProvider {
        readonly Type type;

        /// <summary>
        /// creates a new <see cref="TypeInstanceProvider"/>
        /// </summary>
        /// <param name="type">type to create</param>
        public TypeInstanceProvider(Type type) {
            this.type = type;
        }

        /// <inheritdoc />
        public Type ProvidedType => type;

        /// <inheritdoc />
        public object Create(IScriptToken[] parameters, ScriptContext context) {
            ConstructorInfo[] constructors = type.GetConstructors().Where(c => MethodOperations.MatchesParameterCount(c, parameters)).ToArray();

            if (constructors.Length == 0)
                throw new ScriptRuntimeException($"No matching constructors available for '{type.Name}({string.Join<IScriptToken>(",", parameters)})'", null);

            object[] parametervalues = parameters.Select(p => p.Execute(context)).ToArray();
            Tuple<ConstructorInfo, int>[] evaluated = constructors.Select(c => MethodOperations.GetMethodMatchValue(c, parametervalues)).Where(e => e.Item2 >= 0).ToArray();
            if (evaluated.Length == 0)
                throw new ScriptRuntimeException($"No matching constructor found for '{type.Name}({string.Join(", ", parametervalues)})'", null);

            return MethodOperations.CallConstructor(evaluated[0].Item1, parameters, context);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{type.Name}";
        }
    }
}
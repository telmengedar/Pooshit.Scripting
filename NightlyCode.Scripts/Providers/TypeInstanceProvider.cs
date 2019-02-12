using System;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public object Create(IScriptToken[] parameters, IVariableProvider arguments) {
            ConstructorInfo[] constructors = type.GetConstructors().Where(c => MethodOperations.MatchesParameterCount(c, parameters)).ToArray();

            if (constructors.Length == 0)
                throw new ScriptRuntimeException($"No matching constructors available for '{type.Name}({string.Join<IScriptToken>(",", parameters)})'");

            object[] parametervalues = parameters.Select(p => p.Execute(arguments)).ToArray();
            Tuple<ConstructorInfo, int>[] evaluated = constructors.Select(c => MethodOperations.GetMethodMatchValue(c, parametervalues)).Where(e => e.Item2 >= 0).ToArray();
            if (evaluated.Length == 0)
                throw new ScriptRuntimeException($"No matching constructor found for '{type.Name}({string.Join(", ", parametervalues)})'");

            return MethodOperations.CallConstructor(evaluated[0].Item1, parameters, arguments);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{type.Name}";
        }
    }
}
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
        public object Create(params IScriptToken[] parameters) {
            ConstructorInfo[] constructors = type.GetConstructors().Where(c => MethodOperations.MatchesParameterCount(c, parameters)).ToArray();

            if (constructors.Length == 0)
                throw new ScriptRuntimeException($"No matching constructors available for '{type.Name}({string.Join<IScriptToken>(",", parameters)})'");

            StringBuilder executionlog=new StringBuilder();
            foreach (ConstructorInfo constructor in constructors) {
                try {
                    return MethodOperations.CallConstructor(constructor, parameters);
                }
                catch (Exception e) {
                    executionlog.AppendLine(e.Message);
                }
            }

            throw new ScriptRuntimeException($"Unable to call constructor for '{type.Name}'", executionlog.ToString());
        }
    }
}
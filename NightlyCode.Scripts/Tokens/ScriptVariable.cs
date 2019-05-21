using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// access to variable in script
    /// </summary>
    public class ScriptVariable : AssignableToken {

        /// <summary>
        /// creates a new <see cref="ScriptVariable"/>
        /// </summary>
        /// <param name="name">name of variable</param>
        internal ScriptVariable(string name) {
            Name = name;
        }

        /// <summary>
        /// name of variable
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            if (arguments?.ContainsVariable(Name) ?? false)
                return arguments.GetVariable(Name);

            IVariableProvider provider = variables.GetProvider(Name);
            if (provider == null)
                throw new ScriptRuntimeException($"Variable {Name} not declared");

            return provider.GetVariable(Name);
        }

        /// <inheritdoc />
        protected override object AssignToken(IScriptToken token, IVariableContext variables, IVariableProvider arguments) {
            IVariableProvider provider = variables.GetProvider(Name);
            if (provider == null)
                // auto declare variable in current scope if variable is not found
                provider = variables;

            if (!(provider is IVariableContext context))
                throw new ScriptRuntimeException($"Variable {Name} not writable");

            object value = token.Execute(variables, arguments);
            context.SetVariable(Name, value);
            return value;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"${Name}";
        }
    }
}
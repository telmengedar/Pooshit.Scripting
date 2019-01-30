using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// access to variable in script
    /// </summary>
    public class ScriptVariable : AssignableToken {
        readonly IVariableProvider variablehost;

        /// <summary>
        /// creates a new <see cref="ScriptVariable"/>
        /// </summary>
        /// <param name="variablehost">host containing variable information</param>
        /// <param name="name">name of variable</param>
        internal ScriptVariable(IVariableProvider variablehost, string name) {
            this.variablehost = variablehost;
            Name = name;
        }

        /// <summary>
        /// name of variable
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        protected override object ExecuteToken() {
            IVariableProvider provider = variablehost.GetProvider(Name);
            if (provider == null)
                throw new ScriptRuntimeException($"Variable {Name} not declared");

            return provider.GetVariable(Name);
        }

        /// <inheritdoc />
        protected override object AssignToken(IScriptToken token) {
            IVariableProvider provider = variablehost.GetProvider(Name);
            if (provider == null)
                // auto declare variable in current scope if variable is not found
                provider = variablehost;

            if (!(provider is IVariableContext context))
                throw new ScriptRuntimeException($"Variable {Name} not writable");

            object value = token.Execute();
            context.SetVariable(Name, value);
            return value;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"${Name}";
        }
    }
}
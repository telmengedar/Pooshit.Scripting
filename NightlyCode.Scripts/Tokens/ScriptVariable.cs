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
        /// determines whether the variable was resolved by the parser
        /// </summary>
        public bool IsResolved { get; set; } = true;

        /// <summary>
        /// name of variable
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        public override string Literal => "$value";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            IVariableProvider provider = context.Arguments.GetProvider(Name);
            if (provider != null)
                return provider.GetVariable(Name);

            provider = context.Variables.GetProvider(Name);
            if (provider == null)
                throw new ScriptRuntimeException($"Variable {Name} not declared", this);

            return provider.GetVariable(Name);
        }

        /// <inheritdoc />
        protected override object AssignToken(IScriptToken token, ScriptContext context) {
            IVariableProvider provider = context.Variables.GetProvider(Name);
            if (provider == null)
                // auto declare variable in current scope if variable is not found
                provider = context.Variables;

            if (!(provider is IVariableContext variablecontext))
                throw new ScriptRuntimeException($"Variable {Name} not writable", this);

            object value = token.Execute(context);
            variablecontext.SetVariable(Name, value);
            return value;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"${Name}";
        }
    }
}
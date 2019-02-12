using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// creates a new instance of a type
    /// </summary>
    public class NewInstance : ScriptToken {
        readonly ITypeInstanceProvider provider;
        readonly IScriptToken[] parameters;

        internal NewInstance(ITypeInstanceProvider provider, IScriptToken[] parameters) {
            this.provider = provider;
            this.parameters = parameters;
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments) {
            return provider.Create(parameters, arguments);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"new {provider}({string.Join<IScriptToken>(", ", parameters)})";
        }
    }
}
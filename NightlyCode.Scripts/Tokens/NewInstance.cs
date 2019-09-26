using System.Collections.Generic;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// creates a new instance of a type
    /// </summary>
    public class NewInstance : ScriptToken, IParameterizedToken {
        readonly ITypeInstanceProvider provider;
        readonly IScriptToken[] parameters;

        internal NewInstance(ITypeInstanceProvider provider, IScriptToken[] parameters) {
            this.provider = provider;
            this.parameters = parameters;
        }

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return provider.Create(parameters, context);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"new {provider}({string.Join<IScriptToken>(", ", parameters)})";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters => parameters;
    }
}
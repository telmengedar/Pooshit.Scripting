using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// creates a new instance of a type
    /// </summary>
    public class NewInstance : IScriptToken {
        readonly ITypeInstanceProvider provider;
        readonly IScriptToken[] parameters;

        internal NewInstance(ITypeInstanceProvider provider, IScriptToken[] parameters) {
            this.provider = provider;
            this.parameters = parameters;
        }

        /// <inheritdoc />
        public object Execute() {
            return provider.Create(parameters);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"new {provider}({string.Join<IScriptToken>(", ", parameters)})";
        }
    }
}
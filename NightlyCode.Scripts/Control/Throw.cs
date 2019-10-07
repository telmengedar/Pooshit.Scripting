using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// throws an exception from currently executed code
    /// </summary>
    public class Throw : ScriptToken {
        readonly IScriptToken message;
        readonly IScriptToken context;

        /// <summary>
        /// creates a new <see cref="Throw"/>
        /// </summary>
        /// <param name="message">exception message</param>
        /// <param name="context">context data for exception</param>
        internal Throw(IScriptToken message, IScriptToken context=null) {
            this.message = message;
            this.context = context;
        }

        /// <summary>
        /// exception message
        /// </summary>
        public IScriptToken Message => message;

        /// <summary>
        /// exception context
        /// </summary>
        public IScriptToken Context => context;

        /// <inheritdoc />
        public override string Literal => "throw";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext scriptcontext) {
            throw new ScriptExecutionException(message.Execute(scriptcontext)?.ToString(), Context?.Execute(scriptcontext));
        }

        /// <inheritdoc />
        public override string ToString() {
            if (context == null)
                return $"throw ({message})";
            return $"throw ({message},{context})";
        }
    }
}
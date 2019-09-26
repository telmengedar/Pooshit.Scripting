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
        /// <param name="parameters">parameters for throw</param>
        internal Throw(IScriptToken[] parameters) {
            if(parameters.Length==0)
                throw new ScriptParserException("You need to throw at least a message");
            if (parameters.Length > 2)
                throw new ScriptParserException("Too many arguments for throw");
            message = parameters[0];
            if (parameters.Length > 1)
                context = parameters[1];
        }

        public IScriptToken Message { get; set; }

        public IScriptToken Context { get; set; }

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
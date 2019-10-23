using System;
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
            string messagetext;
            object contextdata;
            try {
                contextdata = Context?.Execute(scriptcontext);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to create context data for throw\n{e.Message}", this, e);
            }

            try {
                messagetext = message.Execute(scriptcontext)?.ToString();
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to create message for throw\n{e.Message}", this, e);
            }

            throw new ScriptRuntimeException(messagetext, this) {
                ContextData = contextdata
            };
        }

        /// <inheritdoc />
        public override string ToString() {
            if (context == null)
                return $"throw ({message})";
            return $"throw ({message},{context})";
        }
    }
}
using System;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// throws an exception from currently executed code
    /// </summary>
    public class Throw : IScriptToken {
        readonly IScriptToken message;
        IScriptToken context;

        /// <summary>
        /// creates a new <see cref="Return"/>
        /// </summary>
        /// <param name="value">token to return</param>
        public Throw(IScriptToken[] parameters) {
            if(parameters.Length==0)
                throw new ScriptParserException("You need to throw at least a message");
            if (parameters.Length > 2)
                throw new ScriptParserException("Too many arguments for throw");
            message = parameters[0];
            if (parameters.Length > 1)
                context = parameters[1];
        }

        /// <inheritdoc />
        public object Execute() {
            throw new ScriptExecutionException(message.Execute()?.ToString(), context?.Execute());
        }

        /// <inheritdoc />
        public override string ToString() {
            if (context == null)
                return $"throw ({message})";
            return $"throw ({message},{context})";
        }
    }
}
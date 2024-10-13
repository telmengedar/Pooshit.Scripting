using System;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// exception triggered when an error was encountered parsing or executing a script
    /// </summary>
    public class ScriptRuntimeException : ScriptException
    {

        /// <summary>
        /// creates a new <see cref="ScriptRuntimeException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="token">token which triggered the error</param>
        /// <param name="innerException">error which lead to this error</param>
        public ScriptRuntimeException(string message, IScriptToken token, Exception innerException = null)
            : base($"{message}", innerException) {
            Token = token;
        }

        /// <summary>
        /// token which triggered the error
        /// </summary>
        public IScriptToken Token { get; }

        /// <summary>
        /// context data for error
        /// </summary>
        public object ContextData { get; set; }
    }
}
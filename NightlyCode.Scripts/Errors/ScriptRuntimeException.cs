using System;

namespace NightlyCode.Scripting.Errors {

    /// <summary>
    /// exception triggered when an error was encountered parsing or executing a script
    /// </summary>
    public class ScriptRuntimeException : ScriptException
    {

        /// <summary>
        /// creates a new <see cref="ScriptRuntimeException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="details">details for error</param>
        /// <param name="innerException">error which lead to this error</param>
        public ScriptRuntimeException(string message, string details = null, Exception innerException = null)
            : base($"{message}\r\n{details}", innerException) {
            Details = details;
        }

        /// <summary>
        /// details for error
        /// </summary>
        public string Details { get; set; }
    }
}
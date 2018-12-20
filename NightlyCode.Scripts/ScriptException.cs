using System;

namespace NightlyCode.Scripting {

    /// <summary>
    /// exception triggered when an error was encountered parsing or executing a script
    /// </summary>
    public class ScriptException : Exception {

        /// <summary>
        /// creates a new <see cref="ScriptException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">error which lead to this error</param>
        public ScriptException(string message, string details = null, Exception innerException = null)
            : base(message, innerException) {
            Details = details;
        }

        /// <summary>
        /// details for error
        /// </summary>
        public string Details { get; set; }
    }
}
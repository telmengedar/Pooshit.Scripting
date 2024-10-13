using System;

namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// exception related to script engine
    /// </summary>
    public abstract class ScriptException : Exception {

        /// <summary>
        /// creates a new <see cref="ScriptException"/>
        /// </summary>
        /// <param name="message">error message</param>
        protected ScriptException(string message) : base(message) {
        }

        /// <summary>
        /// creates a new <see cref="ScriptException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">exception which lead to exception</param>
        protected ScriptException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
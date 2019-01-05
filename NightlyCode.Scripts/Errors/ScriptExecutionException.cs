using System;

namespace NightlyCode.Scripting.Errors {

    /// <summary>
    /// error thrown by script
    /// </summary>
    public class ScriptExecutionException : ScriptException {

        /// <summary>
        /// creates a new <see cref="ScriptException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="data">context data providing info for error</param>
        public ScriptExecutionException(string message, object data=null) 
            : this(message, null, data) {
            
        }

        /// <summary>
        /// creates a new <see cref="ScriptException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">exception which led to this error</param>
        /// <param name="data">context data providing info for error</param>
        public ScriptExecutionException(string message, Exception innerException, object data=null) 
            : base(message,innerException) {
            ContextData = data;
        }

        /// <summary>
        /// context data
        /// </summary>
        public object ContextData { get; set; }
    }
}
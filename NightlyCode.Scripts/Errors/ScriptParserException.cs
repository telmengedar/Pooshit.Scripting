using System;

namespace NightlyCode.Scripting.Errors {

    /// <summary>
    /// exception triggered when parser is unable to parse script
    /// </summary>
    public class ScriptParserException : ScriptException {

        /// <summary>
        /// creates a new <see cref="ScriptParserException"/>
        /// </summary>
        /// <param name="message">error message</param>
        public ScriptParserException(string message) : base(message) {
        }

        /// <summary>
        /// creates a new <see cref="ScriptParserException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">exception which triggered this error</param>
        public ScriptParserException(string message, Exception innerException) 
            : base(message, innerException) {
        }
    }
}
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
        /// <param name="startindex">index where parser error started</param>
        /// <param name="endindex">index where parser error ended</param>
        public ScriptParserException(int startindex, int endindex, string message) : base(message) {
            StartIndex = startindex;
            EndIndex = endindex;
        }

        /// <summary>
        /// creates a new <see cref="ScriptParserException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">exception which triggered this error</param>
        /// <param name="startindex">index where parser error started</param>
        /// <param name="endindex">index where parser error ended</param>
        public ScriptParserException(int startindex, int endindex, string message, Exception innerException) 
            : base(message, innerException) {
            StartIndex = startindex;
            EndIndex = endindex;
        }

        /// <summary>
        /// index where parser error started
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// index where parsing stopped
        /// </summary>
        public int EndIndex { get; set; }
    }
}
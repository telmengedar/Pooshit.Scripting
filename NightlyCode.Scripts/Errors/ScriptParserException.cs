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
        /// <param name="linenumber">line where error was thrown</param>
        public ScriptParserException(int startindex, int endindex, int linenumber, string message) : base(message) {
            StartIndex = startindex;
            EndIndex = endindex;
            Line = linenumber;
        }

        /// <summary>
        /// creates a new <see cref="ScriptParserException"/>
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">exception which triggered this error</param>
        /// <param name="startindex">index where parser error started</param>
        /// <param name="endindex">index where parser error ended</param>
        /// <param name="linenumber">line where error was thrown</param>
        public ScriptParserException(int startindex, int endindex, int linenumber, string message, Exception innerException) 
            : base(message, innerException) {
            StartIndex = startindex;
            EndIndex = endindex;
            Line = linenumber;
        }

        /// <summary>
        /// index where parser error started
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// index where parsing stopped
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// line where error was thrown
        /// </summary>
        public int Line { get; }
    }
}
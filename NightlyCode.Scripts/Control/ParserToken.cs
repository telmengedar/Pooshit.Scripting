using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token containing meta information for parsing process
    /// </summary>
    class ParserToken : ScriptToken {

        /// <summary>
        /// creates a new <see cref="ParserToken"/>
        /// </summary>
        /// <param name="data">data specifying parser case</param>
        public ParserToken(string data) {
            Data = data;
        }

        public string Data { get; }

        protected override object ExecuteToken(ScriptContext context) {
            throw new System.NotImplementedException();
        }

    }
}
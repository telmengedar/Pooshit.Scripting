using System;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

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

        public override string Literal => throw new NotImplementedException("this token is only used internally by the parser");

        protected override object ExecuteToken(ScriptContext context) {
            throw new System.NotImplementedException();
        }

    }
}
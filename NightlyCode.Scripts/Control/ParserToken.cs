namespace NightlyCode.Scripting.Control {
    class ParserToken : IScriptToken {

        /// <summary>
        /// creates a new <see cref="ParserToken"/>
        /// </summary>
        /// <param name="data">data specifying parser case</param>
        public ParserToken(string data) {
            Data = data;
        }
        public string Data { get; }

        public object Execute() {
            throw new System.NotImplementedException();
        }

    }
}
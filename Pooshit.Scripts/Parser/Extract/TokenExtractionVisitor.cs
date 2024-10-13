using System;
using Pooshit.Scripting.Tokens;
using Pooshit.Scripting.Visitors;

namespace Pooshit.Scripting.Parser.Extract {

    /// <summary>
    /// visits a script to extract a token at a specific position
    /// </summary>
    public class TokenExtractionVisitor : ScriptVisitor {
        readonly int position;
        readonly Func<IScriptToken, bool> filter;

        /// <summary>
        /// creates a new <see cref="TokenExtractor"/>
        /// </summary>
        /// <param name="position">position at which to extract token</param>
        /// <param name="filter">filter for tokens to match</param>
        public TokenExtractionVisitor(int position, Func<IScriptToken, bool> filter=null) {
            this.position = position;
            this.filter = filter;
        }

        /// <summary>
        /// extracted token
        /// </summary>
        public ICodePositionToken Token { get; private set; }

        void CheckToken(IScriptToken token) {
            if (token is ICodePositionToken positioninfo && positioninfo.TextIndex <= position && positioninfo.TextIndex >= 0) {
                if (Token != null && positioninfo.TextIndex <= Token.TextIndex)
                    return;

                Token = positioninfo;
            }
        }

        /// <inheritdoc />
        public override void VisitToken(IScriptToken token) {
            if(filter==null||filter(token))
                CheckToken(token);
            base.VisitToken(token);
        }
    }
}
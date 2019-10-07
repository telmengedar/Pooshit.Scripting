using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control.Internal {

    /// <summary>
    /// statement block which serves the body from a func
    /// </summary>
    class FuncStatementBlock : ScriptToken, ITokenContainer {
        readonly Func<IEnumerable<IScriptToken>> body;

        /// <summary>
        /// creates a new <see cref="FuncStatementBlock"/>
        /// </summary>
        /// <param name="body">func serving body tokens</param>
        public FuncStatementBlock(Func<IEnumerable<IScriptToken>> body) {
            this.body = body;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Children => body();

        /// <inheritdoc />
        public override string Literal => "...";

        protected override object ExecuteToken(ScriptContext context) {
            foreach (IScriptToken token in Children)
                token.Execute(context);
            return null;
        }
    }
}
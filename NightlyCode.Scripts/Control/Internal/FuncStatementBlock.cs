using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control.Internal {

    /// <summary>
    /// statement block which serves the body from a func
    /// </summary>
    class FuncStatementBlock : ScriptToken, IStatementBlock {
        readonly Func<IEnumerable<IScriptToken>> body;

        /// <summary>
        /// creates a new <see cref="FuncStatementBlock"/>
        /// </summary>
        /// <param name="body">func serving body tokens</param>
        public FuncStatementBlock(Func<IEnumerable<IScriptToken>> body) {
            this.body = body;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Body => body();

        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            foreach (IScriptToken token in Body)
                token.Execute(variables, arguments);
            return null;
        }
    }
}
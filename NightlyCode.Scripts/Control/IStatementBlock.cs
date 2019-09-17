using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// block containing several statement tokens
    /// </summary>
    public interface IStatementBlock {

        /// <summary>
        /// statements making up body
        /// </summary>
        IEnumerable<IScriptToken> Body { get; }
    }
}
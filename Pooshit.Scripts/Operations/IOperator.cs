using Pooshit.Scripting.Data;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations {

    /// <summary>
    /// scripttoken which operates on other tokens
    /// </summary>
    interface IOperator : IScriptToken {

        /// <summary>
        /// type of operator
        /// </summary>
        Operator Operator { get; }
    }
}
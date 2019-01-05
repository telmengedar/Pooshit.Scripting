using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations {

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
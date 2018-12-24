using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// scripttoken which operates on other tokens
    /// </summary>
    public interface IOperator : IScriptToken {

        /// <summary>
        /// type of operator
        /// </summary>
        Operator Operator { get; }
    }
}
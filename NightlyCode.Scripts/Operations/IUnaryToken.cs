using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// token applying an operation to a single operand
    /// </summary>
    interface IUnaryToken : IScriptToken {

        /// <summary>
        /// determines whether the operand is ahead of this token or behind it
        /// </summary>
        bool IsPostToken { get; }

        /// <summary>
        /// operand for operation
        /// </summary>
        IScriptToken Operand { get; set; }
    }
}
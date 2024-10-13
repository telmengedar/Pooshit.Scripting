using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations {

    /// <summary>
    /// token applying an operation to two operands
    /// </summary>
    public interface IBinaryToken : IScriptToken {
        
        /// <summary>
        /// left hand side operand
        /// </summary>
        IScriptToken Lhs { get; set; }

        /// <summary>
        /// right hand side operand
        /// </summary>
        IScriptToken Rhs { get; set; }
    }
}
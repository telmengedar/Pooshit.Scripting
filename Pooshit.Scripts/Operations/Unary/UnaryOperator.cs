using Pooshit.Scripting.Data;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations.Unary {

    /// <summary>
    /// operator which acts on a single operand which is usually following the operator
    /// </summary>
    public abstract class UnaryOperator : ScriptToken, IUnaryToken, IOperator {

        /// <inheritdoc />
        public virtual bool IsPostToken => false;

        /// <inheritdoc />
        public virtual IScriptToken Operand { get; set; }

        /// <inheritdoc />
        public abstract Operator Operator { get; }
    }
}
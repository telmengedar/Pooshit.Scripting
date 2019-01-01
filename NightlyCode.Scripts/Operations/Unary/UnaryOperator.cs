using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// operator which acts on a single operand which is usually following the operator
    /// </summary>
    public abstract class UnaryOperator : IUnaryToken, IOperator {

        /// <inheritdoc />
        public abstract object Execute();

        public virtual bool IsPostToken => false;

        /// <inheritdoc />
        public IScriptToken Operand { get; set; }

        /// <inheritdoc />
        public abstract Operator Operator { get; }
    }
}
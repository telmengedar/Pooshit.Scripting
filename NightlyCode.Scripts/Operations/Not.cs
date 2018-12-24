using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// logical negation of boolean values
    /// </summary>
    public class Not : IUnaryToken, IOperator {
        /// <inheritdoc />
        public object Execute() {
            object value = Operand.Execute();
            if (value is bool boolean)
                return !boolean;
            throw new ScriptException("'Not' only supported on boolean values");
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public IScriptToken Operand { get; set; }

        public Operator Operator => Operator.Not;
    }
}
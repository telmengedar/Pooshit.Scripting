using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// computes logical XOR of lhs and rhs
    /// </summary>
    public class LogicXor : LogicOperation {
        internal LogicXor() {
        }

        /// <inheritdoc />
        protected override object Operate(ScriptContext context) {
            return Lhs.Execute(context).ToBoolean() != Rhs.Execute(context).ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LogicXor;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} ^^ {Rhs}";
        }
    }
}
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// computes logical OR of lhs and rhs
    /// </summary>
    public class LogicOr : LogicOperation {
        internal LogicOr() {
        }

        /// <inheritdoc />
        protected override object Operate(ScriptContext context) {
            return Lhs.Execute(context).ToBoolean() || Rhs.Execute(context).ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LogicOr;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} || {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "||";
    }
}
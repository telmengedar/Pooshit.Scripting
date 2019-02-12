using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// computes logical OR of lhs and rhs
    /// </summary>
    public class LogicOr : LogicOperation {
        internal LogicOr() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments) {
            return Lhs.Execute(arguments).ToBoolean() || Rhs.Execute(arguments).ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LogicOr;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} || {Rhs}";
        }
    }
}
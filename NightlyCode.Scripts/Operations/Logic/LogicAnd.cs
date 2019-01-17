using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// computes logical AND of lhs and rhs
    /// </summary>
    public class LogicAnd : LogicOperation {
        internal LogicAnd() {
        }

        /// <inheritdoc />
        protected override object Operate() {
            return Lhs.Execute().ToBoolean() && Rhs.Execute().ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LogicAnd;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} && {Rhs}";
        }
    }
}
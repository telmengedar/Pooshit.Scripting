using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class LessOrEqual : Comparator {
        internal LessOrEqual() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs <= (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LessOrEqual;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} <= {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "<=";
    }
}
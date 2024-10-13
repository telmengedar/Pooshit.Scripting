using Pooshit.Scripting.Data;

namespace Pooshit.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class GreaterOrEqual : Comparator {
        internal GreaterOrEqual() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs >= (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.GreaterOrEqual;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} >= {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => ">=";
    }
}
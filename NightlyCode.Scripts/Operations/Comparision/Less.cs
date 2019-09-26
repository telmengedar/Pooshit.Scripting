using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class Less : Comparator {
        internal Less() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs < (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Less;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} < {Rhs}";
        }

    }
}
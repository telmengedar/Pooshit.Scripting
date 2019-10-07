using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise OR on two integer values
    /// </summary>
    public class BitwiseOr : ValueOperation {
        internal BitwiseOr() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs | (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.BitwiseOr;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} | {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "|";
    }
}
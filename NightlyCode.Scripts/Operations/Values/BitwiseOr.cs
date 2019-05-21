using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise OR on two integer values
    /// </summary>
    public class BitwiseOr : ValueOperation {
        internal BitwiseOr() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
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

    }
}
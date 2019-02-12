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
        protected override object Operate(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) | (dynamic)Rhs.Execute(arguments);
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
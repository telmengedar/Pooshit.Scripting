using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// multiplies two values
    /// </summary>
    public class Multiplication : ValueOperation {
        internal Multiplication() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) * (dynamic)Rhs.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Multiplication;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} * {Rhs}";
        }

    }
}
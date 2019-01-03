using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// multiplies two values
    /// </summary>
    public class Multiplication : ValueOperation {

        /// <inheritdoc />
        protected override object Operate()
        {
            return (dynamic)Lhs.Execute() * (dynamic)Rhs.Execute();
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
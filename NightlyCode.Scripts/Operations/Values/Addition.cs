using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// adds two values or concatenates two strings
    /// </summary>
    public class Addition : ValueOperation {

        /// <inheritdoc />
        protected override object Operate() {
            return (dynamic) Lhs.Execute() + (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Addition;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} + {Rhs}";
        }
    }
}
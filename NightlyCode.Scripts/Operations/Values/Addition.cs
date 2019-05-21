using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// adds two values or concatenates two strings
    /// </summary>
    public class Addition : ValueOperation {
        internal Addition() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments) {
            return (dynamic) lhs + (dynamic) rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Addition;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} + {Rhs}";
        }
    }
}
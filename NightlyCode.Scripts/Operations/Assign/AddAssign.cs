using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// adds a value to the result of a token and assigns it at the same time
    /// </summary>
    public class AddAssign : OperatorAssign {
        internal AddAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(IVariableContext variables, IVariableProvider arguments) {
            return (dynamic) Lhs.Execute(variables, arguments) + (dynamic) Rhs.Execute(variables, arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.AddAssign;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} += {Rhs}";
        }
    }
}
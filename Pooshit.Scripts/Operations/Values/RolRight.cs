using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extern;

namespace Pooshit.Scripting.Operations.Values {

    /// <summary>
    /// rolls bits of lhs to the left by rhs steps
    /// </summary>
    public class RolRight : ValueOperation {
        internal RolRight() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context) {
            object value = lhs;
            int steps = Converter.Convert<int>(rhs);

            int numberofbits = value.GetNumberOfBits(this);
            steps = steps % numberofbits;
            if (steps == 0)
                return value;

            object mask = ValueExtensions.GetMask(value.GetType(), numberofbits - steps);
            return (((dynamic)value >> steps) & (dynamic)mask) | ((dynamic)value << (numberofbits - steps));
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.RolRight;

        /// <inheritdoc />
        public override string Literal => ">>>";
    }
}
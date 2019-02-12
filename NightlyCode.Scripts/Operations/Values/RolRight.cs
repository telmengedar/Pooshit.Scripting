using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// rolls bits of lhs to the left by rhs steps
    /// </summary>
    public class RolRight : ValueOperation {
        internal RolRight() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments) {
            object value = Lhs.Execute(arguments);
            int steps = Converter.Convert<int>(Rhs.Execute(arguments));

            int numberofbits = value.GetNumberOfBits();
            steps = steps % numberofbits;
            if (steps == 0)
                return value;

            object mask = ValueExtensions.GetMask(value.GetType(), numberofbits - steps);
            return (((dynamic)value >> steps) & (dynamic)mask) | ((dynamic)value << (numberofbits - steps));
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.RolRight;
    }
}
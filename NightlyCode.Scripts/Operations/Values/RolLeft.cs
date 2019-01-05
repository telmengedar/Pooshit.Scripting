using NightlyCode.Core.Conversion;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// rolls bits of lhs to the left by rhs steps
    /// </summary>
    public class RolLeft : ValueOperation {

        /// <inheritdoc />
        protected override object Operate() {
            object value = Lhs.Execute();
            int steps = Converter.Convert<int>(Rhs.Execute());

            int numberofbits = value.GetNumberOfBits();
            steps = steps % numberofbits;
            if (steps == 0)
                return value;

            object mask = ValueExtensions.GetMask(value.GetType(), steps);
            return ((dynamic)value << steps) | (((dynamic)value >> (numberofbits - steps)) & (dynamic) mask);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.RolLeft;
    }
}
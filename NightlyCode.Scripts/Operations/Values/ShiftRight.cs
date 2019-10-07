using System;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// shifts the bits of LHS by RHS to the right
    /// </summary>
    public class ShiftRight : ValueOperation {
        internal ShiftRight() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context) {
            object value = lhs;
            int steps = rhs.Convert<int>();

            int numberofbits = value.GetNumberOfBits();
            if (steps >= numberofbits)
                return Activator.CreateInstance(value.GetType());

            object mask = ValueExtensions.GetMask(value.GetType(), numberofbits-steps);
            return ((dynamic) value >> (dynamic) steps) & (dynamic) mask;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShiftRight;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} >> {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => ">>";
    }
}
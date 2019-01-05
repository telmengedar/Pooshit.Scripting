using System;
using NightlyCode.Core.Conversion;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// shifts the bits of LHS by RHS to the left
    /// </summary>
    public class ShiftLeft : ValueOperation {

        /// <inheritdoc />
        protected override object Operate()
        {
            object value = Lhs.Execute();
            int steps = Rhs.Execute().Convert<int>();

            int numberofbits = value.GetNumberOfBits();
            if (steps >= numberofbits)
                return Activator.CreateInstance(value.GetType());

            return (dynamic)Lhs.Execute() << (dynamic)Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShiftLeft;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} << {Rhs}";
        }

    }
}
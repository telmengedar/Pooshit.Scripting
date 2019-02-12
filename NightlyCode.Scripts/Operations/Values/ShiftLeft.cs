using System;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// shifts the bits of LHS by RHS to the left
    /// </summary>
    public class ShiftLeft : ValueOperation {
        internal ShiftLeft() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments)
        {
            object value = Lhs.Execute(arguments);
            int steps = Rhs.Execute(arguments).Convert<int>();

            int numberofbits = value.GetNumberOfBits();
            if (steps >= numberofbits)
                return Activator.CreateInstance(value.GetType());

            return (dynamic)Lhs.Execute(arguments) << (dynamic)Rhs.Execute(arguments);
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
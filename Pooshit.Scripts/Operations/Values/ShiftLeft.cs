using System;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;

namespace Pooshit.Scripting.Operations.Values {

    /// <summary>
    /// shifts the bits of LHS by RHS to the left
    /// </summary>
    public class ShiftLeft : ValueOperation {
        internal ShiftLeft() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context)
        {
            object value = lhs;
            int steps = rhs.Convert<int>();

            int numberofbits = value.GetNumberOfBits(this);
            if (steps >= numberofbits)
                return Activator.CreateInstance(value.GetType());

            return (dynamic)lhs << (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShiftLeft;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} << {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "<<";
    }
}
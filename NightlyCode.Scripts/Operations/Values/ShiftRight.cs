﻿using System;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// shifts the bits of LHS by RHS to the right
    /// </summary>
    public class ShiftRight : ValueOperation {
        internal ShiftRight() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments) {
            object value = Lhs.Execute(arguments);
            int steps = Rhs.Execute(arguments).Convert<int>();

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

    }
}
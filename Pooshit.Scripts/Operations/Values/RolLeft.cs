﻿using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Extern;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// rolls bits of lhs to the left by rhs steps
    /// </summary>
    public class RolLeft : ValueOperation {
        internal RolLeft() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context) {
            object value = lhs;
            int steps = Converter.Convert<int>(rhs);

            int numberofbits = value.GetNumberOfBits(this);
            steps = steps % numberofbits;
            if (steps == 0)
                return value;

            object mask = ValueExtensions.GetMask(value.GetType(), steps);
            return ((dynamic)value << steps) | (((dynamic)value >> (numberofbits - steps)) & (dynamic) mask);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.RolLeft;

        /// <inheritdoc />
        public override string Literal => "<<<";
    }
}
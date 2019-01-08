﻿using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// computes logical OR of lhs and rhs
    /// </summary>
    class LogicOr : LogicOperation {

        /// <inheritdoc />
        protected override object Operate() {
            return Lhs.Execute().ToBoolean() || Rhs.Execute().ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LogicOr;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} || {Rhs}";
        }
    }
}
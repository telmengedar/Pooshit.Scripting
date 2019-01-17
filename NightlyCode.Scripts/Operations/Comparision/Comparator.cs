﻿using Microsoft.CSharp.RuntimeBinder;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// operator used to compare two values resulting in a boolean
    /// </summary>
    public abstract class Comparator : ScriptToken, IBinaryToken, IOperator {

        /// <summary>
        /// compares lhs and rhs and returns value of comparision
        /// </summary>
        /// <returns>comparision value</returns>
        protected abstract object Compare();

        /// <inheritdoc />
        protected override object ExecuteToken()
        {
            try
            {
                return Compare();
            }
            catch (RuntimeBinderException e)
            {
                throw new ScriptRuntimeException(e.Message, null, e);
            }
        }

        /// <inheritdoc />
        public IScriptToken Lhs { get; set; }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public abstract Operator Operator { get; }
    }
}
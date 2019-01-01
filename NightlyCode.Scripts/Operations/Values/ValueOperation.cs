using System;
using NightlyCode.Core.Conversion;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// arithmetic operation to apply to two operands
    /// </summary>
    public abstract class ValueOperation : IBinaryToken, IOperator {
        readonly Operator @operator;


        /// <summary>
        /// creates a new <see cref="ValueOperation"/>
        /// </summary>
        /// <param name="operator">operation to apply</param>
        protected ValueOperation(Operator @operator) {
            this.@operator = @operator;
        }

        protected abstract object Operate(byte lhs, byte rhs);

        protected abstract object Operate(short lhs, short rhs);

        protected abstract object Operate(int lhs, int rhs);

        protected abstract object Operate(long lhs, long rhs);

        protected abstract object Operate(float lhs, float rhs);

        protected abstract object Operate(double lhs, double rhs);

        protected abstract object Operate(decimal lhs, decimal rhs);

        public object Execute() {
            object lhs = Lhs.Execute();
            object rhs = Rhs.Execute();

            if (lhs is string || lhs is char || rhs is string || rhs is char) {
                if (@operator != Operator.Addition)
                    throw new ScriptException($"{@operator} not supported on string types");
                return $"{lhs}{rhs}";
            }

            int targetindex = Math.Max(lhs.GetType().GetTypeIndex(), rhs.GetType().GetTypeIndex());
            if (targetindex == -1)
                throw new ScriptException("Arithmetic operation not supported on operands");

            Type targettype = targetindex.GetValueType();
            lhs = Converter.Convert(lhs, targettype);
            rhs = Converter.Convert(rhs, targettype);

            switch (targetindex) {
            case 0:
                return Operate((byte)lhs, (byte)rhs);
            case 1:
                return Operate((short) lhs, (short) rhs);
            case 2:
                return Operate((int)lhs, (int)rhs);
            case 3:
                return Operate((long)lhs, (long)rhs);
            case 4:
                return Operate((float) lhs, (float) rhs);
            case 5:
                return Operate((double) lhs, (double) rhs);
            case 6:
                return Operate((decimal) lhs, (decimal) rhs);
            default:
                throw new ScriptException("Unsupported types for arithmetic operation");
            }
        }

        public Operator Operator => @operator;

        /// <inheritdoc />
        public IScriptToken Lhs { get; set; }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} {Operator.ToStringExt()} {Rhs}";
        }
    }
}
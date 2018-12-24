using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {
    public class BitwiseXor : ValueOperation {

        public BitwiseXor() : base(Operator.BitwiseXor) { }

        protected override object Operate(byte lhs, byte rhs)
        {
            return lhs ^ rhs;
        }

        protected override object Operate(short lhs, short rhs)
        {
            return lhs ^ rhs;
        }

        protected override object Operate(int lhs, int rhs)
        {
            return lhs ^ rhs;
        }

        protected override object Operate(long lhs, long rhs)
        {
            return lhs ^ rhs;
        }

        protected override object Operate(float lhs, float rhs)
        {
            throw new ScriptException("Bitwise AND not supported on float");
        }

        protected override object Operate(double lhs, double rhs)
        {
            throw new ScriptException("Bitwise AND not supported on double");
        }

        protected override object Operate(decimal lhs, decimal rhs)
        {
            throw new ScriptException("Bitwise AND not supported on decimal");
        }

    }
}
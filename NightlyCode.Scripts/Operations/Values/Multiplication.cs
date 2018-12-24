using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    public class Multiplication : ValueOperation {

        public Multiplication() : base(Operator.Multiplication) { }

        protected override object Operate(byte lhs, byte rhs)
        {
            return lhs * rhs;
        }

        protected override object Operate(short lhs, short rhs)
        {
            return lhs * rhs;
        }

        protected override object Operate(int lhs, int rhs)
        {
            return lhs * rhs;
        }

        protected override object Operate(long lhs, long rhs)
        {
            return lhs * rhs;
        }

        protected override object Operate(float lhs, float rhs)
        {
            return lhs * rhs;
        }

        protected override object Operate(double lhs, double rhs)
        {
            return lhs * rhs;
        }

        protected override object Operate(decimal lhs, decimal rhs)
        {
            return lhs * rhs;
        }
    }
}
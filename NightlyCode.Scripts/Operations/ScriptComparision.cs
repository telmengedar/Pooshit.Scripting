using System;
using NightlyCode.Core.Conversion;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// comparision of two operands
    /// </summary>
    public class ScriptComparision : IBinaryToken, IOperator {

        /// <summary>
        /// creates a new <see cref="ScriptComparision"/>
        /// </summary>
        /// <param name="comparator">operator used to compare</param>
        public ScriptComparision(Operator comparator) {
            Operator = comparator;
        }

        public object Execute() {
            object lhs = Lhs.Execute();
            object rhs = Rhs.Execute();

            int targetindex = Math.Max(lhs.GetType().GetTypeIndex(), rhs.GetType().GetTypeIndex());
            if (targetindex == -1)
                throw new ScriptException("Arithmetic operation not supported on operands");

            Type targettype = targetindex.GetValueType();
            lhs = Converter.Convert(lhs, targettype);
            rhs = Converter.Convert(rhs, targettype);

            int result = ((IComparable) lhs).CompareTo(rhs);

            switch (Operator)
            {
            case Operator.Equal:
                return result==0;
            case Operator.NotEqual:
                return result!=0;
            case Operator.Greater:
                return result>0;
            case Operator.GreaterOrEqual:
                return result>=0;
            case Operator.Less:
                return result < 0;
            case Operator.LessOrEqual:
                return result <= 0;
            default:
                throw new ScriptException("Unsupported operator");
            }
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        
        public IScriptToken Lhs { get; set; }
        public IScriptToken Rhs { get; set; }

        public override string ToString() {
            return $"{Lhs} {Operator.ToStringExt()} {Rhs}";
        }

        /// <inheritdoc />
        public Operator Operator { get; private set; }
    }
}
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// comparision of logic tokens
    /// </summary>
    public class LogicComparision : IBinaryToken, IOperator {
        readonly Operator @operator;

        /// <summary>
        /// creates a new <see cref="LogicComparision"/>
        /// </summary>
        /// <param name="operator">logic comparision operator</param>
        public LogicComparision(Operator @operator) {
            this.@operator = @operator;
        }

        /// <summary>
        /// Operator used in operation
        /// </summary>
        public Operator Operator => @operator;

        public object Execute() {
            object lhs = Lhs.Execute();
            object rhs = Rhs.Execute();

            if (!(lhs is bool) || !(rhs is bool))
                throw new ScriptException("Operands of logic comparision must be boolean");

            switch (@operator) {
                case Operator.LogicAnd:
                    return (bool) lhs && (bool) rhs;
                case Operator.LogicOr:
                    return (bool) lhs || (bool) rhs;
                case Operator.LogicXor:
                    return (bool) lhs != (bool) rhs;
                default:
                    throw new ScriptException($"logic operator {@operator} not supported");
            }
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        public IScriptToken Lhs { get; set; }

        public IScriptToken Rhs { get; set; }

        public override string ToString() {
            return $"{Lhs} {@operator.ToStringExt()} {Rhs}";
        }
    }
}
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations {
    public class Negate : IUnaryToken, IOperator {

        public object Execute() {
            object value = Operand.Execute();
            if (value == null)
                throw new ScriptException("Negation of null not supported");

            int targetindex = value.GetType().GetTypeIndex();
            switch (targetindex) {
                case 0:
                    return ~(byte) value;
                case 1:
                    return ~(short) value;
                case 2:
                    return ~(int) value;
                case 3:
                    return ~(long) value;
                default:
                    throw new ScriptException($"Negation of {value.GetType().Name} not supported");
            }
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        public IScriptToken Operand { get; set; }

        public Operator Operator => Operator.Negate;
    }
}
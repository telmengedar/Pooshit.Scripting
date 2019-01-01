using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Values
{

    /// <summary>
    /// get value of a token and increments it afterwards
    /// </summary>
    public class Postcrement : IUnaryToken, IOperator {
        IAssignableToken operand;
        readonly int value;

        /// <summary>
        /// creates a new <see cref="Postcrement"/>
        /// </summary>
        /// <param name="value">value to apply to operand</param>
        public Postcrement(int value)
        {
            this.value = value;
        }

        public object Execute()
        {
            object result = Operand.Execute();
            if (result is int integer)
                operand.Assign(new ScriptValue(integer + value));
            else if (result is long longint)
                operand.Assign(new ScriptValue(longint + value));
            else if (result is float single)
                operand.Assign(new ScriptValue(single + value));
            else if (result is double doublevalue)
                operand.Assign(new ScriptValue(doublevalue + value));
            else if (result is decimal decimalvalue)
                operand.Assign(new ScriptValue(decimalvalue + value));
            else
                throw new ScriptException("Postcrement on non value type");

            return result;
        }

        /// <inheritdoc />
        public bool IsPostToken => true;

        /// <inheritdoc />
        public IScriptToken Operand {
            get => operand;
            set {
                operand=value as IAssignableToken;
                if (operand == null)
                    throw new ScriptException("Operand of postcrement has to be assignable");
            }
        }

        /// <inheritdoc />
        public Operator Operator => Operator.Postcrement;
    }
}
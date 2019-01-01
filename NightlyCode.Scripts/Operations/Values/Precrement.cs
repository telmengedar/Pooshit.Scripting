using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Values
{

    /// <summary>
    /// increases the value of a token and returns the new value
    /// </summary>
    public class Precrement : IUnaryToken, IOperator {
        IAssignableToken operand;
        readonly int value;

        /// <summary>
        /// creates a new <see cref="Postcrement"/>
        /// </summary>
        /// <param name="value">value to apply to operand</param>
        public Precrement(int value)
        {
            this.value = value;
        }

        public object Execute()
        {
            object result = Operand.Execute();
            if (result is int integer)
                result = integer + value;
            else if (result is long longint)
                result = longint + value;
            else if (result is float single)
                result = single + value;
            else if (result is double doublevalue)
                result = doublevalue + value;
            else if (result is decimal decimalvalue)
                result = decimalvalue + value;
            else
                throw new ScriptException("Precrement on non value type");

            operand.Assign(new ScriptValue(result));
            return result;
        }

        /// <inheritdoc />
        public bool IsPostToken => false;

        public IScriptToken Operand {
            get => operand;
            set {
                operand=value as IAssignableToken;
                if (operand == null)
                    throw new ScriptException("Operand of precrement has to be assignable");
            }
        }

        /// <inheritdoc />
        public Operator Operator => Operator.Precrement;

    }
}
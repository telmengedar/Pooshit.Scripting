using System.Linq;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Providers;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token which provides a <see cref="LambdaMethod"/>
    /// </summary>
    public class LambdaToken : IOperator, IBinaryToken {
        IScriptToken lhs;
        IScriptToken rhs;

        /// <inheritdoc />
        public string Literal => "=>";

        /// <summary>
        /// parameters for lambda method
        /// </summary>
        public IScriptToken Lhs {
            get => lhs;
            set {
                lhs = value;
                if(!(lhs is ScriptArray array && array.Values.All(v => v is ScriptVariable)) && !(lhs is ScriptVariable))
                    throw new ScriptParserException(-1, -1, -1, "Lambda parameters must be a single variable or an array of variables");
            }
        }

        /// <summary>
        /// expression to execute
        /// </summary>
        public IScriptToken Rhs {
            get => rhs;
            set {
                if(value is StatementBlock block)
                    // ensure that method block is true for return to be evaluated correctly
                    block.MethodBlock = true;
                rhs = value;
            }
        }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            string[] parameters;
            if(Lhs is ScriptArray parameterarray)
                parameters = parameterarray.Values.Cast<ScriptVariable>().Select(v => v.Name).ToArray();
            else
                parameters = new[] { ((ScriptVariable)Lhs).Name };

            ScriptContext lambdacontext = new ScriptContext(context);
            return new LambdaMethod(parameters, lambdacontext, Rhs);
        }

        /// <inheritdoc />
        public Operator Operator => Operator.Lambda;
    }
}
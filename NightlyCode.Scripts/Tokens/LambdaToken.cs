using System.Linq;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Providers;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token which provides a <see cref="LambdaMethod"/>
    /// </summary>
    public class LambdaToken : IOperator, IBinaryToken {
        IScriptToken lhs;

        /// <inheritdoc />
        public string Literal => "()=>{}";

        /// <summary>
        /// parameters for lambda method
        /// </summary>
        public IScriptToken Lhs {
            get => lhs;
            set {
                lhs = value;
                if (lhs is ScriptArray array) {
                    if(array.Values.Any(v=>!(v is ScriptVariable)))
                        throw new ScriptParserException(-1,-1,"Lambda parameters must be a single variable or an array of variables");

                    foreach (ScriptVariable var in array.Values.Cast<ScriptVariable>())
                        var.IsResolved = true;
                }
                else if (lhs is ScriptVariable variable)
                    variable.IsResolved = true;
                else throw new ScriptParserException(-1,-1,"Lambda parameters must be a single variable or an array of variables");
            }
        }

        /// <summary>
        /// expression to execute
        /// </summary>
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            string[] parameters;
            if (Lhs is ScriptArray parameterarray)
                parameters = parameterarray.Values.Cast<ScriptVariable>().Select(v => v.Name).ToArray();
            else parameters = new[] {((ScriptVariable) Lhs).Name};

            ScriptContext lambdacontext = new ScriptContext(new VariableContext(context.Variables), context.Arguments, context.CancellationToken);
            return new LambdaMethod(parameters, lambdacontext, Rhs);
        }

        /// <inheritdoc />
        public T Execute<T>(ScriptContext context) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Operator Operator => Operator.Lambda;
    }
}
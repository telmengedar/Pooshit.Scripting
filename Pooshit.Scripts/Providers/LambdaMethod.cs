using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Providers {

    /// <summary>
    /// lambda method which can get executed
    /// </summary>
    public class LambdaMethod {
        readonly string[] parameters;
        readonly ScriptContext context;
        readonly IScriptToken expression;

        /// <summary>
        /// creates a new <see cref="LambdaMethod"/>
        /// </summary>
        /// <param name="parameters">parameters to use for lambda</param>
        /// <param name="context">execution context</param>
        /// <param name="expression">expression to execute</param>
        public LambdaMethod(string[] parameters, ScriptContext context, IScriptToken expression) {
            this.parameters = parameters;
            this.context = context;
            this.expression = expression;
        }

        /// <summary>
        /// invokes the method
        /// </summary>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        public object Invoke(params object[] arguments) {
            if(parameters.Length != arguments.Length)
                throw new ScriptRuntimeException($"Argument count doesn't match up parameter count:\n{string.Join(", ", parameters)}", expression);

            ScriptContext lambdacontext = new ScriptContext(context);
            for(int i = 0; i < parameters.Length; ++i)
                lambdacontext.Arguments[parameters[i]] = arguments[i];

            return expression.Execute(lambdacontext);
        }
    }
}
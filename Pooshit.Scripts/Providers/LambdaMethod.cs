using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Providers {

    /// <summary>
    /// lambda method which can get executed
    /// </summary>
    public class LambdaMethod : IExternalMethod {
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
        /// invokes the method using this lambda's own captured context for its depth budget; prefer
        /// <see cref="InvokeFrom"/> when an invoking context is available
        /// </summary>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        public object Invoke(params object[] arguments) {
            CheckArguments(arguments);
            context.Guard();
            return InvokeCore(context.DepthBudget, arguments);
        }

        /// <summary>
        /// invokes the method given an explicit invoking context, resolving the depth budget from
        /// <paramref name="invokingContext"/> instead of this lambda's own captured context
        /// </summary>
        /// <param name="invokingContext">context of the call site invoking this lambda</param>
        /// <param name="arguments">arguments for lambda</param>
        /// <returns>execution result</returns>
        public object InvokeFrom(ScriptContext invokingContext, params object[] arguments) {
            CheckArguments(arguments);
            invokingContext.Guard();
            return InvokeCore(invokingContext.DepthBudget, arguments);
        }

        /// <inheritdoc />
        object IExternalMethod.Invoke(ScriptContext invokingContext, params object[] arguments) => InvokeFrom(invokingContext, arguments);

        /// <summary>
        /// invokes the method as the first frame of a new physical call stack (a <c>task.run</c> body on its
        /// own thread pool thread), so depth is measured against just this stack rather than the caller's
        /// </summary>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        internal object InvokeOnNewStack(params object[] arguments) {
            CheckArguments(arguments);
            context.Guard();
            DepthBudget freshBudget = context.DepthBudget == null ? null : new DepthBudget(context.DepthBudget.Limit);
            return InvokeCore(freshBudget, arguments);
        }

        /// <summary>
        /// validates that <paramref name="arguments"/> matches this lambda's parameter count; a <c>null</c>
        /// array is treated as zero arguments
        /// </summary>
        /// <param name="arguments">arguments to validate, or <c>null</c> (treated as zero arguments)</param>
        void CheckArguments(object[] arguments) {
            int argumentCount = arguments?.Length ?? 0;
            if(parameters.Length != argumentCount)
                throw new ScriptRuntimeException($"Argument count doesn't match up parameter count:\n{string.Join(", ", parameters)}", expression);
        }

        /// <summary>
        /// shared invocation body for <see cref="Invoke"/>, <see cref="InvokeFrom"/> and
        /// <see cref="InvokeOnNewStack"/>; enters and exits <paramref name="depthBudget"/> around the call
        /// </summary>
        /// <param name="depthBudget">depth budget to enter for this invocation, or <c>null</c> when unconfigured</param>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        object InvokeCore(DepthBudget depthBudget, object[] arguments) {
            try {
                depthBudget?.Enter();

                ScriptContext lambdacontext = new ScriptContext(context, depthBudget);
                for(int i = 0; i < parameters.Length; ++i)
                    lambdacontext.Arguments[parameters[i]] = arguments[i];

                return expression.Execute(lambdacontext);
            }
            finally {
                depthBudget?.Exit();
            }
        }
    }
}

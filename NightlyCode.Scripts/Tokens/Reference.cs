using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// a reference to an assignable token
    /// </summary>
    public class Reference : IAssignableToken {
        IAssignableToken argument;

        /// <summary>
        /// creates a new <see cref="Reference"/>
        /// </summary>
        /// <param name="argument">argument to use as reference parameter</param>
        internal Reference(IAssignableToken argument) {
            this.argument = argument;
        }

        /// <inheritdoc />
        public object Execute(IVariableContext variables, IVariableProvider arguments) {
            return argument.Execute(variables, arguments);
        }

        /// <inheritdoc />
        public T Execute<T>(IVariableContext variables, IVariableProvider arguments) {
            return argument.Execute<T>(variables, arguments);
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token, IVariableContext variables, IVariableProvider arguments) {
            return argument.Assign(token, variables, arguments);
        }
    }
}
using System.Threading.Tasks;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Providers {

    /// <summary>
    /// provides a method to execute in a thread
    /// </summary>
    public class TaskProvider : ITypeInstanceProvider {

        /// <inheritdoc />
        public object Create(IScriptToken[] parameters, IVariableContext variables, IVariableProvider arguments) {
            if (parameters.Length != 1)
                throw new ScriptRuntimeException("Threads can only contain the method body as parameter");

            return new Task<object>(() => parameters[0].Execute(variables, arguments));
        }
    }
}
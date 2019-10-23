using System;
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
        public Type ProvidedType => typeof(Task<object>);

        /// <inheritdoc />
        public object Create(IScriptToken[] parameters, ScriptContext context) {
            if (parameters.Length != 1)
                throw new ScriptRuntimeException("Threads can only contain the method body as parameter", null);

            return new Task<object>(() => parameters[0].Execute(context));
        }
    }
}
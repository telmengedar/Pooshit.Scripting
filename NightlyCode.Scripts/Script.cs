using System;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting {

    /// <summary>
    /// script parsed by <see cref="ScriptParser"/>
    /// </summary>
    class Script : IScript {
        readonly StatementBlock script;

        /// <summary>
        /// creates a new <see cref="Script"/>
        /// </summary>
        /// <param name="script">root token of script to be executed</param>
        internal Script(StatementBlock script) {
            this.script = script;
        }

        /// <inheritdoc />
        public object Execute(params Variable[] variables) {
            if (variables?.Length > 0)
                return script.Execute(new VariableProvider(null, variables));

            return script.Execute();
        }

        /// <inheritdoc />
        public T Execute<T>(params Variable[] variables) {
            object result = Execute(variables);
            if (result is T execute)
                return execute;

            try {
                return Converter.Convert<T>(result);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert return value of script to {nameof(T)}", null, e);
            }
        }
    }
}
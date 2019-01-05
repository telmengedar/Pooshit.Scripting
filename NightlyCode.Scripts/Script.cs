using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// script parsed by <see cref="ScriptParser"/>
    /// </summary>
    class Script : IScript {
        readonly IScriptToken script;

        /// <summary>
        /// creates a new <see cref="Script"/>
        /// </summary>
        /// <param name="script">root token of script to be executed</param>
        internal Script(IScriptToken script) {
            this.script = script;
        }

        /// <inheritdoc />
        public object Execute() {
            return script.Execute();
        }

        /// <inheritdoc />
        public T Execute<T>() {
            object result = script.Execute();
            try {
                return Converter.Convert<T>(result);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert return value of script to {nameof(T)}", null, e);
            }
        }
    }
}
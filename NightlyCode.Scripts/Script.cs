using System;
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// script parsed by <see cref="ScriptParser"/>
    /// </summary>
    class Script : IScript {
        readonly IVariableProvider scriptvariables;
        readonly IScriptToken script;

        /// <summary>
        /// creates a new <see cref="Script"/>
        /// </summary>
        /// <param name="script">root token of script to be executed</param>
        /// <param name="scriptvariables">access to script variables</param>
        internal Script(IScriptToken script, IVariableProvider scriptvariables) {
            this.script = script;
            this.scriptvariables = scriptvariables;
        }

        T ConvertResult<T>(object result) {
            if (result is T execute)
                return execute;

            try {
                return Converter.Convert<T>(result);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert return value of script to {nameof(T)}", null, e);
            }
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(CancellationToken cancellationtoken, params Variable[] variables) {
            object result = await ExecuteAsync(cancellationtoken, variables);
            return ConvertResult<T>(result);
        }

        /// <summary>
        /// script body
        /// </summary>
        public IScriptToken Body => script;

        /// <inheritdoc />
        public object Execute(params Variable[] variables) {
            if (variables?.Length > 0)
                return script.Execute(new ScriptContext(new VariableContext(scriptvariables), new VariableProvider(null, variables)));
            return script.Execute(new ScriptContext(new VariableContext(scriptvariables), null));
        }

        /// <inheritdoc />
        public Task<object> ExecuteAsync(CancellationToken cancellationtoken, params Variable[] variables) {
            return Task.Run(() => {
                if (variables?.Length > 0)
                    return script.Execute(new ScriptContext(new VariableContext(scriptvariables), new VariableProvider(null, variables), cancellationtoken));
                return script.Execute(new ScriptContext(new VariableContext(scriptvariables), null, cancellationtoken));
            }, cancellationtoken);
        }

        /// <inheritdoc />
        public T Execute<T>(params Variable[] variables) {
            object result = Execute(variables);
            return ConvertResult<T>(result);
        }
    }
}
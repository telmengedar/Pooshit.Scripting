using System;
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// script parsed by <see cref="ScriptParser"/>
    /// </summary>
    class Script : IScript {
        ITypeProvider typeprovider;
        readonly IScriptToken script;

        /// <summary>
        /// creates a new <see cref="Script"/>
        /// </summary>
        /// <param name="script">root token of script to be executed</param>
        /// <param name="typeprovider">access to installed types</param>
        internal Script(IScriptToken script, ITypeProvider typeprovider) {
            this.script = script;
            this.typeprovider = typeprovider;
        }

        T ConvertResult<T>(object result) {
            if(result is T execute)
                return execute;

            try {
                return Converter.Convert<T>(result);
            }
            catch(Exception e) {
                throw new ScriptRuntimeException($"Unable to convert return value of script to {nameof(T)}", null, e);
            }
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(IVariableProvider variables = null, CancellationToken cancellationtoken = default) {
            object result = await ExecuteAsync(variables, cancellationtoken);
            return ConvertResult<T>(result);
        }

        /// <summary>
        /// script body
        /// </summary>
        public IScriptToken Body => script;

        /// <inheritdoc />
        public object Execute(IVariableProvider variables = null) {
            return script.Execute(new ScriptContext(variables, typeprovider));
        }

        /// <inheritdoc />
        public Task<object> ExecuteAsync(IVariableProvider variables = null, CancellationToken cancellationtoken = default) {
            return Task.Run(() => script.Execute(new ScriptContext(variables, typeprovider, cancellationtoken)), cancellationtoken);
        }

        /// <inheritdoc />
        public T Execute<T>(IVariableProvider variables = null) {
            object result = Execute(variables);
            return ConvertResult<T>(result);
        }
    }
}
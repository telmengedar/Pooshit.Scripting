using System;
using System.Collections.Generic;
using System.Text;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// block which handles a disposable resource
    /// </summary>
    public class Using : ControlToken, IParameterContainer {
        readonly IScriptToken[] disposables;

        internal Using(IScriptToken[] disposables) {
            this.disposables = disposables;
        }

        /// <inheritdoc />
        public override string Literal => "using";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            List<IDisposable> values=new List<IDisposable>();
            try {
                foreach (IScriptToken token in disposables) {
                    object value = token.Execute(context);
                    if (!(value is IDisposable disposablevalue))
                        throw new ScriptRuntimeException($"'{token}' does not evaluate to an idisposable", token);
                    values.Add(disposablevalue);
                }

                return Body.Execute(context);
            }
            finally {
                StringBuilder log=new StringBuilder();
                foreach (IDisposable value in values) {
                    try {
                        value.Dispose();
                    }
                    catch (Exception e) {
                        log.AppendLine($"{value}: {e.Message}");
                    }
                }

                if (log.Length > 0)
                    throw new ScriptRuntimeException($"Error disposing values: {log}", this);
            }
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"using({string.Join<IScriptToken>(",", disposables)}) {Body}";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters => disposables;

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}
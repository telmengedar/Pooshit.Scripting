using System;
using System.Collections.Generic;
using System.Text;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

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
        /// <remarks>
        /// Tracks whether an exception (including a host cancellation) is already unwinding via the local
        /// <c>faulted</c> flag, so the dispose-failure report below never replaces it. A <c>throw</c> from
        /// a <c>finally</c> block replaces the in-flight exception outright, which would otherwise silently
        /// destroy a caller's cancellation and defeat the very watchdog that raised it.
        /// </remarks>
        protected override object ExecuteToken(ScriptContext context) {
            List<IDisposable> values=new List<IDisposable>();
            bool faulted = false;
            try {
                foreach (IScriptToken token in disposables) {
                    object value = token.Execute(context);
                    if (!(value is IDisposable disposablevalue))
                        throw new ScriptRuntimeException($"'{token}' does not evaluate to an idisposable", token);
                    values.Add(disposablevalue);
                }

                return Body.Execute(context);
            }
            catch {
                faulted = true;
                throw;
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

                if (log.Length > 0 && !faulted)
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
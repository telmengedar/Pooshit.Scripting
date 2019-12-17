using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {
    /// <summary>
    /// a block of statements executed in sequence
    /// </summary>
    public class StatementBlock : ITokenContainer, ICodePositionToken {
        readonly IScriptToken[] statements;

        /// <summary>
        /// creates a new <see cref="StatementBlock"/>
        /// </summary>
        /// <param name="statements">statements in block</param>
        /// <param name="methodblock">determines whether this is the main block of a method</param>
        internal StatementBlock(IScriptToken[] statements, bool methodblock=false) {
            this.statements = statements;
            MethodBlock = methodblock;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Children => statements;

        /// <inheritdoc />
        public string Literal => "{ ... }";

        /// <summary>
        /// determines whether block is a method block
        /// </summary>
        public bool MethodBlock { get; internal set; }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            try {
                return ExecuteBlock(context);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException(e.Message, this, e);
            }
        }

        object ExecuteBlock(ScriptContext context) {
            ScriptContext blockcontext = new ScriptContext(new VariableContext(context.Variables), context.Arguments, context.CancellationToken);
            object result = null;
            foreach (IScriptToken statement in statements)
            {
                blockcontext.CancellationToken.ThrowIfCancellationRequested();

                try {
                    result = statement.Execute(blockcontext);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (ScriptException) {
                    throw;
                }
                catch (Exception e) {
                    throw new ScriptRuntimeException($"Unable to execute '{statement}': {e.Message}", this, e);
                }

                if (result is Return @return)
                {
                    if (MethodBlock)
                        return @return.Value?.Execute(blockcontext);
                    return @return;
                }

                if (result is Break || result is Continue)
                    return result;

            }

            return result;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{{ {string.Join<IScriptToken>("\n", statements)} }}";
        }

        /// <inheritdoc />
        public int LineNumber { get; internal set; }

        /// <inheritdoc />
        public int TextIndex { get; internal set;}

        /// <inheritdoc />
        public int TokenLength { get; internal set; }
    }
}
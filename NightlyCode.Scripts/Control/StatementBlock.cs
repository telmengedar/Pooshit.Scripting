using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {
    /// <summary>
    /// a block of statements executed in sequence
    /// </summary>
    public class StatementBlock : ScriptToken, ITokenContainer {
        readonly IScriptToken[] statements;
        readonly bool methodblock;

        /// <summary>
        /// creates a new <see cref="StatementBlock"/>
        /// </summary>
        /// <param name="statements">statements in block</param>
        /// <param name="methodblock">determines whether this is the main block of a method</param>
        internal StatementBlock(IScriptToken[] statements, bool methodblock=false) {
            this.statements = statements;
            this.methodblock = methodblock;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Children => statements;

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            return ExecuteBlock(context);
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
                catch (ScriptException) {
                    throw;
                }
                catch (Exception e) {
                    throw new ScriptExecutionException($"Unable to execute '{statement}': {e.Message}", e);
                }

                if (result is Return @return)
                {
                    if (methodblock)
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
    }
}
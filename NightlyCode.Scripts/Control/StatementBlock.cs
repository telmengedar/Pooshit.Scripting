using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// a block of statements executed in sequence
    /// </summary>
    public class StatementBlock : ScriptToken {
        readonly IVariableContext variablecontext;
        readonly IScriptToken[] statements;
        readonly bool methodblock;

        /// <summary>
        /// creates a new <see cref="StatementBlock"/>
        /// </summary>
        /// <param name="variablecontext">variable environment for this block</param>
        /// <param name="statements">statements in block</param>
        /// <param name="methodblock">determines whether this is the main block of a method</param>
        internal StatementBlock(IScriptToken[] statements, IVariableContext variablecontext=null, bool methodblock=false) {
            this.variablecontext = variablecontext;
            this.statements = statements;
            this.methodblock = methodblock;
        }

        /// <inheritdoc />
        protected override object ExecuteToken()
        {
            object result;
            if (variablecontext != null) {
                
                using (variablecontext) {
                    result=ExecuteBlock();
                }
                variablecontext.Clear();
            }
            else result=ExecuteBlock();
            return result;
        }

        object ExecuteBlock() {
            object result = null;
            foreach (IScriptToken statement in statements)
            {
                try {
                    result = statement.Execute();
                }
                catch (Exception e) {
                    throw new ScriptExecutionException($"Unable to execute '{statement}': {e.Message}", e);
                }

                if (result is Return @return)
                {
                    if (methodblock)
                        return @return.Value?.Execute();
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
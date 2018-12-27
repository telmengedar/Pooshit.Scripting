namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// a block of statements executed in sequence
    /// </summary>
    public class StatementBlock : IScriptToken {
        readonly IScriptToken[] statements;
        readonly bool methodblock;

        /// <summary>
        /// creates a new <see cref="StatementBlock"/>
        /// </summary>
        /// <param name="statements">statements in block</param>
        /// <param name="methodblock">determines whether this is the main block of a method</param>
        public StatementBlock(IScriptToken[] statements, bool methodblock=false) {
            this.statements = statements;
            this.methodblock = methodblock;
        }

        /// <inheritdoc />
        public object Execute() {
            object result = null;
            foreach (IScriptToken statement in statements) {
                result = statement.Execute();
                if (result is Return @return) {
                    if (methodblock)
                        return @return.Value?.Execute();
                    return @return;
                }
            }

            return result;
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }
    }
}
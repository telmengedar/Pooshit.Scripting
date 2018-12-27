namespace NightlyCode.Scripting.Control {
    public class StatementBlock : IScriptToken {
        readonly IScriptToken[] statements;

        public StatementBlock(IScriptToken[] statements) {
            this.statements = statements;
        }

        public object Execute() {
            object result = null;
            foreach (IScriptToken statement in statements)
                result = statement.Execute();
            return result;
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }
    }
}
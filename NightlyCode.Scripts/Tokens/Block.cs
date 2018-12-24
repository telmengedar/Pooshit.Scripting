namespace NightlyCode.Scripting.Tokens {
    public class Block : IScriptToken {
        readonly IScriptToken inner;

        public Block(IScriptToken inner) {
            this.inner = inner;
        }

        public object Execute() {
            return inner.Execute();
        }

        public object Assign(IScriptToken token) {
            return inner.Assign(token);
        }

        public override string ToString() {
            return $"({inner})";
        }
    }
}
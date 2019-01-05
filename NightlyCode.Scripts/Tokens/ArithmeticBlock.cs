namespace NightlyCode.Scripting.Tokens {
    class ArithmeticBlock : IScriptToken {
        readonly IScriptToken inner;

        public ArithmeticBlock(IScriptToken inner) {
            this.inner = inner;
        }

        public object Execute() {
            return inner.Execute();
        }

        public override string ToString() {
            return $"({inner})";
        }
    }
}
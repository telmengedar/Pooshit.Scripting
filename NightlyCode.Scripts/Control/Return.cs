namespace NightlyCode.Scripting.Control {
    public class Return : IScriptToken {
        readonly IScriptToken value;

        public Return(IScriptToken value) {
            this.value = value;
        }

        public IScriptToken Value => value;

        public object Execute() {
            return this;
        }

        public override string ToString() {
            return $"return {value}";
        }
    }
}
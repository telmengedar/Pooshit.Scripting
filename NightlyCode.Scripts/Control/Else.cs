namespace NightlyCode.Scripting.Control {
    public class Else : IControlToken {
        public object Execute() {
            throw new System.NotImplementedException();
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        public IScriptToken Body { get; set; }
    }
}
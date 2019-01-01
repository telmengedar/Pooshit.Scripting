namespace NightlyCode.Scripting.Control {
    public class Else : IControlToken {
        public object Execute() {
            throw new System.NotImplementedException();
        }

        public IScriptToken Body { get; set; }
    }
}
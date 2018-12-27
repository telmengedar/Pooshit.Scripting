using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Control {
    public class While : IControlToken {
        readonly IScriptToken condition;

        public While(IScriptToken[] parameters) {
            if (parameters.Length != 1)
                throw new ScriptException("While needs exactly one condition as parameter");
            condition = parameters[0];
        }

        public object Execute() {
            while (condition.Execute().ToBoolean()) {
                Body.Execute();
            }

            return null;
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        public IScriptToken Body { get; set; }
    }
}
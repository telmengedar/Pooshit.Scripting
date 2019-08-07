using NightlyCode.Scripting.Parser;

namespace Script.Executor {

    public class ScriptEnvironment {
        IScriptParser parser;

        public ScriptEnvironment(IScriptParser parser) {
            this.parser = parser;
        }

        //public void Import()
    }
}
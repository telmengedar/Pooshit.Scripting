using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ArgumentTests {
        readonly IScriptParser parser = new ScriptParser();

        [Parallelizable]
        [TestCase(7, 14)]
        [TestCase(20, 27)]
        [TestCase(0, 7)]
        public void PrecompileAndExecuteWithDifferentArguments(int argument, int expected) {
            IScript script = parser.Parse(
                "$result=7\n" +
                "$result+=$argument\n" +
                "$result"
            );

            Assert.AreEqual(expected, script.Execute(new VariableProvider(new Variable("argument", argument))));
        }
    }
}
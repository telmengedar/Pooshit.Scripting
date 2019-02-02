using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class OperatorTests {
        readonly IScriptParser parser=new ScriptParser();

        [Test, Parallelizable]
        public void CompareWithNegativeNumber() {
            IScript script = parser.Parse("-1==-1");
            Assert.AreEqual(true, script.Execute());
        }

        [Test, Parallelizable]
        public void DecrementGreater() {
            IScript script = parser.Parse(
                "$count=5\n" +
                "$result=0\n" +
                "while($count-->0)\n" +
                "  ++$result\n" +
                "$result"
            );
            Assert.AreEqual(5, script.Execute());
        }
    }
}
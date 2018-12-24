using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class UnaryOperationTests {

        [Test, Description("Logical negation of expressions")]
        public void Not() {
            ScriptParser parser = new ScriptParser(new ScriptHostPool());
            Assert.AreEqual(false, parser.Parse("!true").Execute());
            Assert.AreEqual(true, parser.Parse("!false").Execute());
        }

        [Test]
        public void Negate() {
            ScriptParser parser = new ScriptParser(new ScriptHostPool());
            Assert.AreEqual(~6, parser.Parse("~6").Execute());
        }
    }
}
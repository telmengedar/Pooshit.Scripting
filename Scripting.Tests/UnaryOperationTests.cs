using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class UnaryOperationTests {

        [Test, Description("Logical negation of expressions")]
        public void Not() {
            ScriptParser parser = new ScriptParser();
            Assert.AreEqual(false, parser.Parse("!true").Execute());
            Assert.AreEqual(true, parser.Parse("!false").Execute());
        }

        [Test]
        public void Negate() {
            ScriptParser parser = new ScriptParser();
            Assert.AreEqual(~6, parser.Parse("~6").Execute());
        }

        [Test]
        public void Postcrement() {
            TestHost testhost = new TestHost();
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "host[\"value\"]=5\n" +
                "host[\"value\"]++");
            Assert.AreEqual(5, script.Execute(new VariableProvider(new Variable("host", testhost))));
            Assert.AreEqual(6, testhost["value"]);
        }

        [Test]
        public void Precrement() {
            TestHost testhost = new TestHost();
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "host[\"value\"]=5\n" +
                "++host[\"value\"]");
            Assert.AreEqual(6, script.Execute(new VariableProvider(new Variable("host", testhost))));
            Assert.AreEqual(6, testhost["value"]);
        }
    }
}
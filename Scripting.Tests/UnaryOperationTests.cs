using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;
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
        public void Postcrement()
        {
            TestHost testhost=new TestHost();
            ScriptParser parser = new ScriptParser(new Variable("host", testhost));
            IScript script = parser.Parse(
                "host[\"value\"]=5\n" +
                "host[\"value\"]++");
            Assert.AreEqual(5, script.Execute());
            Assert.AreEqual(6, testhost["value"]);
        }

        [Test]
        public void Precrement()
        {
            TestHost testhost = new TestHost();
            ScriptParser parser = new ScriptParser(new Variable("host", testhost));
            IScript script = parser.Parse(
                "host[\"value\"]=5\n" +
                "++host[\"value\"]");
            Assert.AreEqual(6, script.Execute());
            Assert.AreEqual(6, testhost["value"]);
        }
    }
}
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class LogicOperationTests {
        readonly ScriptParser parser = new ScriptParser();

        [TestCase("3<4&&8>2", true)]
        [TestCase("3>4&&8>2", false)]
        [TestCase("3<4&&8<2", false)]
        [TestCase("3>4&&8<2", false)]
        [Parallelizable]
        public void TestAnd(string data, bool expected) {
            IScript script = parser.Parse(data);
            Assert.AreEqual(expected, script.Execute());
        }

        [TestCase("3<4||8>2", true)]
        [TestCase("3>4||8>2", true)]
        [TestCase("3<4||8<2", true)]
        [TestCase("3>4||8<2", false)]
        [Parallelizable]
        public void TestOr(string data, bool expected) {
            IScript script = parser.Parse(data);
            Assert.AreEqual(expected, script.Execute());
        }

        [TestCase("3<4^^8>2", false)]
        [TestCase("3>4^^8>2", true)]
        [TestCase("3<4^^8<2", true)]
        [TestCase("3>4^^8<2", false)]
        [Parallelizable]
        public void TestXor(string data, bool expected)
        {
            IScript script = parser.Parse(data);
            Assert.AreEqual(expected, script.Execute());
        }

    }
}
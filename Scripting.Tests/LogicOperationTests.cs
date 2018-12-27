using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class LogicOperationTests {

        [Test]
        public void TestAnd() {
            Assert.AreEqual(true, new ScriptParser(new ScriptHosts()).Parse("3<4&&8>2").Execute());
        }

        [Test]
        public void TestOr()
        {
            Assert.AreEqual(true, new ScriptParser(new ScriptHosts()).Parse("3<4&&8>2").Execute());
        }

        [Test]
        public void TestXor()
        {
            Assert.AreEqual(false, new ScriptParser(new ScriptHosts()).Parse("3<4^^8>2").Execute());
        }

    }
}
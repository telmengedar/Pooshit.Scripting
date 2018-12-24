using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class KeywordTests {

        [Test]
        public void True() {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse("true").Execute());
        }

        [Test]
        public void False() {
            Assert.AreEqual(false, new ScriptParser(new ScriptHostPool()).Parse("false").Execute());
        }
    }
}
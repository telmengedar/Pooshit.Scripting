using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class KeywordTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test]
        public void True() {
            Assert.AreEqual(true, parser.Parse("true").Execute());
        }

        [Test]
        public void False() {
            Assert.AreEqual(false, parser.Parse("false").Execute());
        }
    }
}
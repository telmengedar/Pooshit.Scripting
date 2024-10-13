using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class KeywordTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void True() {
            Assert.AreEqual(true, parser.Parse("true").Execute());
        }

        [Test, Parallelizable]
        public void False() {
            Assert.AreEqual(false, parser.Parse("false").Execute());
        }
    }
}
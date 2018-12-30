using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class BitwiseOperationTests {

        [Test]
        public void BitwiseAnd() {
            Assert.AreEqual(27&13, new ScriptParser(new ExtensionProvider()).Parse("27&13").Execute());
        }

        [Test]
        public void BitwiseOr() {
            Assert.AreEqual(27 | 13, new ScriptParser(new ExtensionProvider()).Parse("27|13").Execute());
        }

        [Test]
        public void BitwiseXor() {
            Assert.AreEqual(27 ^ 13, new ScriptParser(new ExtensionProvider()).Parse("27^13").Execute());
        }

        [Test]
        public void BitwiseShiftLeft()
        {
            Assert.AreEqual(27 << 2, new ScriptParser(new ExtensionProvider()).Parse("27<<2").Execute());
        }

        [Test]
        public void BitwiseShiftRight()
        {
            Assert.AreEqual(27 >> 1, new ScriptParser(new ExtensionProvider()).Parse("27>>1").Execute());
        }
    }
}
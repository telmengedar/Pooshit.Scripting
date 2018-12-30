using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ArithmeticTests {

        [Test, Parallelizable]
        public void Addition() {
            Assert.AreEqual(7, new ScriptParser(new ExtensionProvider()).Parse("3+4").Execute());
        }

        [Test, Parallelizable]
        public void Subtraction()
        {
            Assert.AreEqual(-1, new ScriptParser(new ExtensionProvider()).Parse("3-4").Execute());
        }

        [Test, Parallelizable]
        public void Division()
        {
            Assert.AreEqual(3, new ScriptParser(new ExtensionProvider()).Parse("12/4").Execute());
        }

        [Test, Parallelizable]
        public void Multiplication()
        {
            Assert.AreEqual(12, new ScriptParser(new ExtensionProvider()).Parse("3*4").Execute());
        }

        [Test, Parallelizable]
        public void Modulo()
        {
            Assert.AreEqual(1, new ScriptParser(new ExtensionProvider()).Parse("5%4").Execute());
        }

        [Test, Parallelizable]
        public void OperatorPriority() {
            Assert.AreEqual(5+12-3*2+6/8%5, new ScriptParser(new ExtensionProvider()).Parse("5+12-3*2+6/8%5").Execute());
        }

        [Test, Parallelizable]
        public void Blocks() {
            Assert.AreEqual(120, new ScriptParser(new ExtensionProvider()).Parse("(4+8)*(2+8)").Execute());
        }
    }
}
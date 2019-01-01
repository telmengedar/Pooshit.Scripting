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

        [Test, Parallelizable]
        public void OperationWithPostAndPrecrements()
        {
            ScriptParser parser=new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=5\n" +
                "++$value+$value++ + --$value+$value--\n"
            );

            Assert.AreEqual(24, script.Execute());
        }

        [Test, Parallelizable]
        public void AddAssign() {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=5\n" +
                "$value+=5\n" +
                "$value"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [Test, Parallelizable]
        public void SubAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=5\n" +
                "$value-=3\n" +
                "$value"
            );
            Assert.AreEqual(2, script.Execute());
        }

        [Test, Parallelizable]
        public void MulAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=5\n" +
                "$value*=3\n" +
                "$value"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void DivAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value/=2\n" +
                "$value"
            );
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void ModAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value%=8\n" +
                "$value"
            );
            Assert.AreEqual(2, script.Execute());
        }

        [Test, Parallelizable]
        public void ShlAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value<<=2\n" +
                "$value"
            );
            Assert.AreEqual(40, script.Execute());
        }

        [Test, Parallelizable]
        public void ShrAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value>>=1\n" +
                "$value"
            );
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void AndAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value&=2\n" +
                "$value"
            );
            Assert.AreEqual(2, script.Execute());
        }

        [Test, Parallelizable]
        public void OrAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value|=7\n" +
                "$value"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void XorAssign()
        {
            ScriptParser parser = new ScriptParser();
            IScriptToken script = parser.Parse(
                "$value=10\n" +
                "$value^=80\n" +
                "$value"
            );
            Assert.AreEqual(10^80, script.Execute());
        }
    }
}
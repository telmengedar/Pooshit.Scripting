using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class BitwiseOperationTests {
        IScriptParser parser = new ScriptParser();

        [Test]
        public void BitwiseAnd() {
            Assert.AreEqual(27 & 13, parser.Parse("27&13").Execute());
        }

        [Test]
        public void BitwiseOr() {
            Assert.AreEqual(27 | 13, parser.Parse("27|13").Execute());
        }

        [Test]
        public void BitwiseXor() {
            Assert.AreEqual(27 ^ 13, parser.Parse("27^13").Execute());
        }

        [TestCase("27<<2", 27<<2)]
        [TestCase("8<<32", 0)]
        [TestCase("8<<33", 0)]
        [TestCase("1073741824<<1", -2147483648)]
        public void BitwiseShiftLeft(string data, object expected) {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }

        [TestCase("27>>1", 27 >> 1)]
        [TestCase("-1>>1", int.MaxValue)]
        [TestCase("16>>32", 0)]
        [TestCase("16>>33", 0)]
        [Parallelizable]
        public void BitwiseShiftRight(string data, object expected)
        {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void NoShiftPrioritites()
        {
            IScript shiftleftfirst = parser.Parse("-1<<1>>1");
            IScript shiftrightfirst = parser.Parse("-1>>1<<1");
            Assert.AreNotEqual(shiftleftfirst.Execute(), shiftrightfirst.Execute());
        }

        [TestCase("1>>>1", int.MinValue)]
        [TestCase("1>>>31", 2)]
        [TestCase("1>>>32", 1)]
        [TestCase("1>>>62", 4)]
        [Parallelizable]
        public void RolRight(string script, object expected) {
            Assert.AreEqual(expected, parser.Parse(script).Execute());
        }

        [TestCase("-2147483647<<<1", 3)]
        [Parallelizable]
        public void RolLeft(string script, object expected) {
            Assert.AreEqual(expected, parser.Parse(script).Execute());
        }
    }
}
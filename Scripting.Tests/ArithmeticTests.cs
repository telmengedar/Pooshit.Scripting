using System;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ArithmeticTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void Addition() {
            Assert.AreEqual(7, parser.Parse("3+4").Execute());
        }

        [Test, Parallelizable]
        public void Subtraction() {
            Assert.AreEqual(-1, parser.Parse("3-4").Execute());
        }

        [Test, Parallelizable]
        public void Division() {
            Assert.AreEqual(3, parser.Parse("12/4").Execute());
        }

        [Test, Parallelizable]
        public void Multiplication() {
            Assert.AreEqual(12, parser.Parse("3*4").Execute());
        }

        [Test, Parallelizable]
        public void Modulo() {
            Assert.AreEqual(1, parser.Parse("5%4").Execute());
        }

        [Test, Parallelizable]
        public void OperatorPriority() {
            Assert.AreEqual(5 + 12 - 3 * 2 + 6 / 8 % 5, parser.Parse("5+12-3*2+6/8%5").Execute());
        }

        [Test, Parallelizable]
        public void Blocks() {
            Assert.AreEqual(120, parser.Parse("(4+8)*(2+8)").Execute());
        }

        [Test, Parallelizable]
        public void OperationWithPostAndPrecrements() {
            IScript script = parser.Parse(
                "$value=5\n" +
                "++$value+$value++ + --$value+$value--\n"
            );

            Assert.AreEqual(24, script.Execute());
        }

        [Test, Parallelizable]
        public void AddAssign() {
            IScript script = parser.Parse(
                "$value=5\n" +
                "$value+=5\n" +
                "$value"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [Test, Parallelizable]
        public void SubAssign() {
            IScript script = parser.Parse(
                "$value=5\n" +
                "$value-=3\n" +
                "$value"
            );
            Assert.AreEqual(2, script.Execute());
        }

        [Test, Parallelizable]
        public void MulAssign() {
            IScript script = parser.Parse(
                "$value=5\n" +
                "$value*=3\n" +
                "$value"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void DivAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value/=2\n" +
                "$value"
            );
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void ModAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value%=8\n" +
                "$value"
            );
            Assert.AreEqual(2, script.Execute());
        }

        [Test, Parallelizable]
        public void ShlAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value<<=2\n" +
                "$value"
            );
            Assert.AreEqual(40, script.Execute());
        }

        [Test, Parallelizable]
        public void ShrAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value>>=1\n" +
                "$value"
            );
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void AndAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value&=2\n" +
                "$value"
            );
            Assert.AreEqual(2, script.Execute());
        }

        [Test, Parallelizable]
        public void OrAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value|=7\n" +
                "$value"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void XorAssign() {
            IScript script = parser.Parse(
                "$value=10\n" +
                "$value^=80\n" +
                "$value"
            );
            Assert.AreEqual(10 ^ 80, script.Execute());
        }

        [Test, Parallelizable]
        public void AddDoubleToDecimal() {
            IScript script = parser.Parse(
                "10.0d+10.0"
            );
            Assert.AreEqual(20.0m, script.Execute());
        }

        [Test, Parallelizable]
        public void AddTimespanToDatetime() {
            IScript script = parser.Parse("$date+$time");
            Assert.DoesNotThrow(() => script.Execute(new VariableProvider(new Variable("date", DateTime.Now), new Variable("time", TimeSpan.FromHours(1.0)))));
        }
    }
}
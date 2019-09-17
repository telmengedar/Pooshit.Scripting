using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class OperatorTests {
        readonly IScriptParser parser=new ScriptParser();

        public Operator GetOperator(Operator op) {
            return op;
        }

        [Test, Parallelizable]
        public void CompareWithNegativeNumber() {
            IScript script = parser.Parse("-1==-1");
            Assert.AreEqual(true, script.Execute());
        }

        [Test, Parallelizable]
        public void DecrementGreater() {
            IScript script = parser.Parse(
                "$count=5\n" +
                "$result=0\n" +
                "while($count-->0)\n" +
                "  ++$result\n" +
                "$result"
            );
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void CompareEnumWithString() {
            IScript script = parser.Parse("$operator!=\"Not\"", new Variable("operator", Operator.Not));
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable]
        public void CompareEnumWithInt() {
            IScript script = parser.Parse("$operator!=0", new Variable("operator", Operator.Not));
            Assert.DoesNotThrow(() => script.Execute());
        }

        [TestCase("2", "\"3\"", "23")]
        [TestCase("\"7\"", "2", "72")]
        [TestCase("2", "3.6", 5.6)]
        [Parallelizable]
        public void ConvertOperantsToHighestPrecision(string lhs, string rhs, object expectation) {
            IScript script = parser.Parse($"{lhs}+{rhs}");
            Assert.AreEqual(expectation, script.Execute());
        }

        [Test, Parallelizable]
        public void ChangeOperators() {
            ScriptParser changedparser=new ScriptParser();
            changedparser.OperatorTree.Clear();

            changedparser.OperatorTree.Add("=", Operator.Equal);

            IScript script = changedparser.Parse("1=1");
            Assert.AreEqual(true, script.Execute());
        }
    }
}
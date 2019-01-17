using NightlyCode.Scripting;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests
{
    [TestFixture, Parallelizable]
    public class StringOperationTests {
        readonly IScriptParser parser = new ScriptParser();

        [TestCase("\"test\"-\"_1\"")]
        [TestCase("\"test\"/\"_1\"")]
        [TestCase("\"test\"*\"_1\"")]
        [TestCase("\"test\"%\"_1\"")]
        [TestCase("\"test\">>\"_1\"")]
        [TestCase("\"test\"<<\"_1\"")]
        [TestCase("\"test\"&\"_1\"")]
        [TestCase("\"test\"|\"_1\"")]
        [TestCase("\"test\"^\"_1\"")]
        [Parallelizable]
        public void InvalidStringOperations(string data)
        {
            IScript script = parser.Parse(data);
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, Parallelizable]
        public void Concatenate()
        {
            IScript script = parser.Parse("\"test\"+\"_1\"");
            Assert.AreEqual("test_1", script.Execute());
        }

        [Test, Parallelizable]
        public void LogicAnd() {
            Assert.AreEqual(true, parser.Parse("\"test\"&&\"wrong\"").Execute());
        }

        [Test, Parallelizable]
        public void LogicOr()
        {
            Assert.AreEqual(false, parser.Parse("\"\"&&null").Execute());
        }

        [Test, Parallelizable]
        public void LogicXor()
        {
            Assert.AreEqual(false, parser.Parse("\"test\"^^\"test7\"").Execute());
        }

        [Test, Parallelizable]
        public void PlusAssign() {
            IScript script = parser.Parse(
                "$value=\"\"\n" +
                "$value+=\"bla\""
            );

            Assert.AreEqual("bla", script.Execute());
        }
    }
}
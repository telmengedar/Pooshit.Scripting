using NightlyCode.Scripting;
using NightlyCode.Scripting.Errors;
using NUnit.Framework;

namespace Scripting.Tests
{
    [TestFixture, Parallelizable]
    public class StringOperationTests
    {

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
            ScriptParser parser=new ScriptParser();
            IScriptToken script = parser.Parse(data);
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, Parallelizable]
        public void Concatenate()
        {
            ScriptParser parser=new ScriptParser();
            IScriptToken script = parser.Parse("\"test\"+\"_1\"");
            Assert.AreEqual("test_1", script.Execute());
        }

        [Test, Parallelizable]
        public void LogicAnd() {
            Assert.AreEqual(true, new ScriptParser().Parse("\"test\"&&\"wrong\"").Execute());
        }

        [Test, Parallelizable]
        public void LogicOr()
        {
            Assert.AreEqual(false, new ScriptParser().Parse("\"\"&&null").Execute());
        }

        [Test, Parallelizable]
        public void LogicXor()
        {
            Assert.AreEqual(false, new ScriptParser().Parse("\"test\"^^\"test7\"").Execute());
        }
    }
}
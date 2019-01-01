using NightlyCode.Scripting;
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
        [TestCase("\"test\"&&\"_1\"")]
        [TestCase("\"test\"||\"_1\"")]
        [TestCase("\"test\"^^\"_1\"")]
        [Parallelizable]
        public void InvalidStringOperations(string data)
        {
            ScriptParser parser=new ScriptParser();
            IScriptToken script = parser.Parse(data);
            Assert.Throws<ScriptException>(() => script.Execute());
        }

        [Test, Parallelizable]
        public void Concatenate()
        {
            ScriptParser parser=new ScriptParser();
            IScriptToken script = parser.Parse("\"test\"+\"_1\"");
            Assert.AreEqual("test_1", script.Execute());
        }
    }
}
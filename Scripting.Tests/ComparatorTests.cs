using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ComparatorTests {

        [TestCase("3==3")]
        [TestCase("8.2==8.2")]
        [TestCase("\"string\"==\"string\"")]
        [Parallelizable]
        public void Equals(string data) {
            Assert.AreEqual(true, new ScriptParser(new ExtensionProvider()).Parse(data).Execute());
        }

        [TestCase("3!=7")]
        [TestCase("8.2!=8.9")]
        [TestCase("\"string\"!=\"strong\"")]
        [Parallelizable]
        public void NotEquals(string data)
        {
            Assert.AreEqual(true, new ScriptParser(new ExtensionProvider()).Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void Less()
        {
            Assert.AreEqual(true, new ScriptParser(new ExtensionProvider()).Parse("3<8").Execute());
        }

        [TestCase("3<=3")]
        [TestCase("1<=3")]
        [Parallelizable]
        public void LessEquals(string data)
        {
            Assert.AreEqual(true, new ScriptParser(new ExtensionProvider()).Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void Greater()
        {
            Assert.AreEqual(true, new ScriptParser(new ExtensionProvider()).Parse("8>4").Execute());
        }

        [Test, Parallelizable]
        public void MethodGreater() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            Assert.AreEqual(true, parser.Parse("test.integer(7)>2").Execute());
        }

        [Test, Parallelizable]
        public void PropertyGreater() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScriptToken script=parser.Parse(
                "test.property=8\n"+
                "test.property>5"
            );
            Assert.AreEqual(true, script.Execute());
        }

        [TestCase("3>=3")]
        [TestCase("8>=4")]
        [Parallelizable]
        public void GreaterEqualsEquals(string data)
        {
            Assert.AreEqual(true, new ScriptParser(new ExtensionProvider()).Parse(data).Execute());
        }

    }
}
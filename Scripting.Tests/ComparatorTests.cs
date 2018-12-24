using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ComparatorTests {

        [TestCase("3==3")]
        [TestCase("8.2==8.2")]
        [TestCase("\"string\"==\"string\"")]
        [Parallelizable]
        public void Equals(string data) {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse(data).Execute());
        }

        [TestCase("3!=7")]
        [TestCase("8.2!=8.9")]
        [TestCase("\"string\"!=\"strong\"")]
        [Parallelizable]
        public void NotEquals(string data)
        {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void Less()
        {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse("3<8").Execute());
        }

        [TestCase("3<=3")]
        [TestCase("1<=3")]
        [Parallelizable]
        public void LessEquals(string data)
        {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void Greater()
        {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse("8>4").Execute());
        }

        [TestCase("3>=3")]
        [TestCase("8>=4")]
        [Parallelizable]
        public void GreaterEqualsEquals(string data)
        {
            Assert.AreEqual(true, new ScriptParser(new ScriptHostPool()).Parse(data).Execute());
        }

    }
}
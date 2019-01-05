using NightlyCode.Scripting;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class CharacterTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void MultipleCharactersFail() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("'cda'"));
        }

        [Test, Parallelizable]
        public void ParseCharacter() {
            IScript script = parser.Parse("'a'");
            Assert.AreEqual('a', script.Execute());
        }

        [TestCase("'\t'", '\t')]
        [TestCase("'\r'", '\r')]
        [TestCase("'\n'", '\n')]
        [Parallelizable]
        public void ParseSpecialCharacters(string data, char expected) {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }
    }
}
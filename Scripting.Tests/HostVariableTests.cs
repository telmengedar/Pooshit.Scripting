using NUnit.Framework;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class HostVariableTests {
        readonly IScriptParser parser;

        public HostVariableTests() {
            parser = new ScriptParser();
        }

        [Test, Parallelizable]
        public void ReturnHost() {
            Assert.DoesNotThrow(() => parser.Parse("return (host)"));
        }

    }
}
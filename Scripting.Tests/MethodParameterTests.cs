
using System.Collections.Generic;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class MethodParameterTests {
        IScriptParser parser;

        [SetUp]
        public void StartUp() {
            parser = new ScriptParser();
        }

        public string Enumeration(IEnumerable<string> parameters) {
            return string.Join(";", parameters);
        }

        [Test, Parallelizable]
        public void EnumerationParameterCall() {
            IScript script = parser.Parse("test.enumeration([\"hello\",\"world\"])");
            Assert.AreEqual("hello;world", script.Execute(new VariableProvider(new Variable("test", this))));
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ParameterTests {
        IScriptParser parser;

        [SetUp]
        public void StartUp() {
            parser = new ScriptParser(new Variable("test", this));
        }

        public string Enumeration(IEnumerable<string> parameters) {
            return string.Join(";", parameters);
        }

        [Test, Parallelizable]
        public void EnumerationParameterCall() {
            IScript script = parser.Parse("test.enumeration([\"hello\",\"world\"])");
            Assert.AreEqual("hello;world", script.Execute());
        }
    }
}
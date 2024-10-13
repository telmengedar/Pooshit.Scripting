using System.Collections.Generic;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {
    
    [TestFixture, Parallelizable]
    public class ComplexStatementTests {

        [Test, Parallelizable]
        public void AndOrChain() {
            IScriptParser parser = new ScriptParser();

            string code = ScriptCode.Create(
                "$budget={",
                "\"value\": 0",
                "}",
                "$data = {",
                " \"referencenumber\": {",
                "  \"value\": \"\"",
                "}}",
                "$reference=$data.referencenumber",
                "return($budget!=null&&budget.value==0&&($reference.value==\"1\"||$reference.value==\"2\"||$reference.value==\"3\"||$reference.value==\"4\"||$reference.value==\"5\"||$reference.value==\"6\"||$reference.value==\"7\"))"
            );

            IScript script = parser.Parse(code);
            Assert.False(script.Execute<bool>());
        }
    }
}
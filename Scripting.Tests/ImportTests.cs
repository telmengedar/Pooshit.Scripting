﻿using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Providers;

namespace Scripting.Tests {

    [TestFixture]
    public class ImportTests {

        [Test, Parallelizable]
        public void ExecuteImportedScript() {
            ScriptParser parser = new ScriptParser();
            parser.ImportProvider = new ResourceScriptMethodProvider(typeof(ImportTests).Assembly, parser);
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$method=import(\"Scripting.Tests.Scripts.External.increasedbyone.ns\")",
                    "return($method.invoke(10))"
                ));

            Assert.AreEqual(11, script.Execute());
        }

        [Test, Parallelizable]
        public void ImplicitExecute() {
            ScriptParser parser = new ScriptParser();
            parser.ImportProvider = new ResourceScriptMethodProvider(typeof(ImportTests).Assembly, parser);
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$method=import(\"Scripting.Tests.Scripts.External.increasedbyone.ns\")",
                    "return($method(10))"
                ));

            Assert.AreEqual(11, script.Execute());
        }

        [TestCase("Scripting.Tests.Scripts.External.initialization.ns")]
        [Parallelizable]
        public void ImportAndExecuteExternal(string resource) {
            ScriptParser parser = new ScriptParser();
            parser.ImportProvider = new ResourceScriptMethodProvider(typeof(ImportTests).Assembly, parser);
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$method=import($script)",
                    "$method()"
                ));

            Assert.DoesNotThrow(() => script.Execute(new VariableProvider(new Variable("script", resource))));
        }

        [Test, Parallelizable]
        public void ImportsInImports() {

        }
    }
}
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Providers;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture]
    public class ImportTests {

        [Test, Parallelizable]
        public void ExecuteImportedScript() {
            ScriptParser parser=new ScriptParser();
            parser.MethodResolver = new ResourceScriptMethodProvider(typeof(ImportTests).Assembly, parser);
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$method=import(\"Scripting.Tests.Scripts.External.increasedbyone.ns\")",
                    "return($method.invoke(10))"
                ));

            Assert.AreEqual(11, script.Execute());
        }
    }
}
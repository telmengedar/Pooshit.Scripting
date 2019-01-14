using System.Collections;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ConstructorTests {
        readonly IScriptParser parser = new ScriptParser();

        [SetUp]
        public void Setup() {
            parser.Types.AddType<Variable>("variable");
        }

        [Test, Parallelizable]
        public void CreateSingleParameterLeavingOutDefault() {
            IScript script = parser.Parse(
                "$var=new variable(\"test\")\n" +
                "$var.name"
            );
            Assert.AreEqual("test", script.Execute());
        }

        [Test, Parallelizable]
        public void CreateVariableWithAllParameters() {
            IScript script = parser.Parse(
                "$var=new variable(\"test\", [1,2,7])\n" +
                "$var.value"
            );
            Assert.That(new[] { 1, 2, 7 }.SequenceEqual(script.Execute<IEnumerable>().Cast<int>()));
        }
    }
}
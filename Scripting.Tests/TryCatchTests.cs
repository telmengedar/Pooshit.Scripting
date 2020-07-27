using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TryCatchTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void TryWithoutCatch() {
            IScript script = parser.Parse("try throw(\"error\") return(0)");
            Assert.AreEqual(0, script.Execute());
        }

        [Test, Parallelizable]
        public void TryWithCatch() {
            IScript script = parser.Parse("try throw(\"error\") catch return($exception.message) return(0)");
            Assert.AreEqual("error", script.Execute());
        }

        [Test, Parallelizable]
        public void VariablesArePropagatedToCatch() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$failurecode=5",
                "try",
                "  throw(\"error\")",
                "catch",
                "  return($failurecode)",
                "return(0)"));
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void VariablesArePropagatedToCatchBlock() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$failurecode=5",
                "try",
                "  throw(\"error\")",
                "catch {",
                "  return($failurecode)",
                "}",
                "return(0)"));
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void VariablesArePropagatedToCatchInterpolation() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$failurecode=5",
                "$result=null",
                "try",
                "  throw(\"error\")",
                "catch {",
                "  $result=$\"Nani {$failurecode}\"",
                "}",
                "return($result)"));
            Assert.AreEqual("Nani 5", script.Execute());
        }

        [Test, Parallelizable]
        public void VariablesArePropagatedToThrowInCatch() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$failurecode=5",
                "$result=null",
                "try",
                "  throw(\"error\")",
                "catch {",
                "  throw($failurecode)",
                "}",
                "return($result)"));
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, Parallelizable]
        public void ParametersArePropagatedToCatch() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($failurecode, \"int\")",
                "try",
                "  throw(\"error\")",
                "catch",
                "  return($failurecode)",
                "return(0)"));
            Assert.AreEqual(5, script.Execute(new VariableProvider(new Variable("failurecode", 5))));
        }
    }
}
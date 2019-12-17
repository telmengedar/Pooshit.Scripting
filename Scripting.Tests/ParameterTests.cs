using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ParameterTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void PutFloatingPointToIntegerType() {
            IScript script = parser.Parse("parameter($input, \"int\") return($input)");
            Assert.AreEqual(12, script.Execute(new Variable("input", 12.23)));
        }

        [Test, Parallelizable]
        public void SpecifyUnknownType() {
            Assert.Throws<ScriptParserException>(()=>parser.Parse("parameter($input, \"ont\") return($input)"));
        }

        [Test, Parallelizable]
        public void ArrayParameter() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($collection, \"int[]\")",
                "$result=0",
                "foreach($number,$collection)",
                "  $result+=$number",
                "return($result)"
            ));

            Assert.AreEqual(6, script.Execute(new Variable("collection", new[] {1, 2, 3})));
        }

        [Test, Parallelizable]
        public void FreeTypeParameter() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($collection, \"NightlyCode.Scripting.Data.Variable,NightlyCode.Scripting[]\")",
                "$result=0",
                "foreach($number,$collection)",
                "  $result+=$number.value",
                "return($result)"
            ));

            Assert.AreEqual(6, script.Execute(new Variable("collection", new[] {
                new Variable("first", 1),
                new Variable("second", 2),
                new Variable("third", 3)
            })));
        }

        [Test, Parallelizable]
        public void DefaultParameterValueSpecified() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($value, \"int\", 5)",
                "return($value*$value)"
            ));

            Assert.AreEqual(9, script.Execute(new Variable("value", 3)));
        }

        [Test, Parallelizable]
        public void DefaultParameterValueNotSpecified() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($value, \"int\", 5)",
                "return($value*$value)"
            ));

            Assert.AreEqual(25, script.Execute());
        }

        [Test, Parallelizable]
        public void TypedArrayParameterFromObjectArray() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($value, \"int[]\")",
                "return($value)"
            ));

            Assert.AreEqual(typeof(int[]), script.Execute(new Variable("value", new object[] {1, 2, 3})).GetType());
        }

        [Test, Parallelizable]
        public void TypedArrayParameterFromEnumeration() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($value, \"int[]\")",
                "return($value)"
            ));

            Assert.AreEqual(typeof(int[]), script.Execute(new Variable("value", new object[] {1, 2, 3}.Select(v => v))).GetType());
        }
    }
}
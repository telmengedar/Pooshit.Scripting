using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ParameterTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void CustomTypeName() {
            IScriptParser customparser=new ScriptParser();
            customparser.Types.AddType<string>("onk");
            IScript script = customparser.Parse("parameter($input, onk) return($input)");
            Assert.AreEqual("haha", script.Execute(new VariableProvider(new Variable("input", "haha"))));
        }

        [Test, Parallelizable]
        public void PutFloatingPointToIntegerType() {
            IScript script = parser.Parse("parameter($input, int) return($input)");
            Assert.AreEqual(12, script.Execute(new VariableProvider(new Variable("input", 12.23))));
        }

        [Test, Parallelizable]
        public void PutFloatingPointToIntegerTypeString() {
            IScript script = parser.Parse("parameter($input, \"int\") return($input)");
            Assert.AreEqual(12, script.Execute(new VariableProvider(new Variable("input", 12.23))));
        }

        [Test, Parallelizable]
        public void SpecifyUnknownType() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("parameter($input, \"ont\") return($input)"));
        }

        [Test, Parallelizable]
        public void ArrayParameter() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($collection, int[])",
                "$result=0",
                "foreach($number,$collection)",
                "  $result+=$number",
                "return($result)"
            ));

            Assert.AreEqual(6, script.Execute(new VariableProvider(new Variable("collection", new[] { 1, 2, 3 }))));
        }

        [Test, Parallelizable]
        public void ArrayParameterString() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($collection, \"int[]\")",
                "$result=0",
                "foreach($number,$collection)",
                "  $result+=$number",
                "return($result)"
            ));

            Assert.AreEqual(6, script.Execute(new VariableProvider(new Variable("collection", new[] { 1, 2, 3 }))));
        }

        [Test, Parallelizable]
        public void FreeTypeParameter() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($collection, \"Pooshit.Scripting.Data.Variable,Pooshit.Scripting[]\")",
                "$result=0",
                "foreach($number,$collection)",
                "  $result+=$number.value",
                "return($result)"
            ));

            Assert.AreEqual(6, script.Execute(new VariableProvider(new Variable("collection", new[] {
                new Variable("first", 1),
                new Variable("second", 2),
                new Variable("third", 3)
            }))));
        }

        [Test, Parallelizable]
        public void DefaultParameterValueSpecified() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($value, \"int\", 5)",
                "return($value*$value)"
            ));

            Assert.AreEqual(9, script.Execute(new VariableProvider(new Variable("value", 3))));
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

            Assert.AreEqual(typeof(int[]), script.Execute(new VariableProvider(new Variable("value", new object[] { 1, 2, 3 }))).GetType());
        }

        [Test, Parallelizable]
        public void TypedArrayParameterFromEnumeration() {
            IScript script = parser.Parse(ScriptCode.Create(
                "parameter($value, \"int[]\")",
                "return($value)"
            ));

            Assert.AreEqual(typeof(int[]), script.Execute(new VariableProvider(new Variable("value", new object[] { 1, 2, 3 }.Select(v => v)))).GetType());
        }
        
        [Test, Parallelizable]
        public void CustomTypeFromJsonDictionary() {
            IScriptParser customparser=new ScriptParser();
            customparser.Types.AddType<Parameter>("lonk");
            
            IScript script = customparser.Parse(ScriptCode.Create(
                "parameter($value, \"lonk\")",
                "return($value.name)"
            ));

            Assert.AreEqual("lars", script.Execute(new VariableProvider(new Variable("value", new Dictionary<string, object> {
                ["Name"]="lars"
            }))));
        }

        [Test, Parallelizable]
        public void ObjectParameterUsedInIf() {
            IScriptParser customParser=new ScriptParser();

            IScript script = customParser.Parse(
                                                @"parameter($material, object, null) if($material!=null) return(material.thickness) return(2.5)"
                                               );

            Assert.AreEqual(2.5, script.Execute());
        }
    }
}
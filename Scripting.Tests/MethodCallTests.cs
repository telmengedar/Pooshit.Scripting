using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class MethodCallTests {
        readonly IScriptParser parser = new ScriptParser();

        public string MethodWithSingleParameter(string prefix, Parameter parameter) {
            return $"{prefix}:{parameter.Name}={parameter.Value}";
        }

        public string MethodWithArrayParameters(string prefix, params Parameter[] parameters) {
            return $"{prefix}:{string.Join<Parameter>(",", parameters)}";
        }

        public string MethodWithDefaults(string name, string title = null, params string[] additional) {
            return $"{title} {name} {string.Join(" ", additional)}";
        }

        public string Ambigious(byte[] bytearray) {
            return "bytearray";
        }

        public string Ambigious(string parameter) {
            return "string";
        }

        public string Ambigious(int parameter) {
            return "int";
        }

        public string InterfaceParameter(IParameter parameter) {
            return "parameter";
        }

        public string EnumParameter(TestEnum @enum) {
            return "enum";
        }

        public void RefParameter(ref int parameter) {
            parameter = 42;
        }

        public void OutParameter(out int parameter) {
            parameter = 42;
        }

        public string GuidParameter(Guid guid) {
            return guid.ToString();
        }

        public string NullableGuidParameter(Guid? guid) {
            return guid?.ToString();
        }

        public void RandomListMethod(bool first = false, Guid? second = null, Guid? third = null, int[] last = null) {

        }

        public T Convert<T>(object parameter) {
            return (T)System.Convert.ChangeType(parameter, typeof(T), CultureInfo.InvariantCulture);
        }

        public Parameter Parameter(string name, string value) {
            return new Parameter {
                Name = name,
                Value = value
            };
        }

        public object DictionaryMethod(Dictionary<string, object> parameter) {
            if (parameter == null)
                return null;
            parameter.TryGetValue("result", out object result);
            return result;
        }
        
        [Test, Parallelizable]
        public void CallMethodWithArrayParameters() {
            IScript script = parser.Parse("$test.methodwitharrayparameters(\"success\", [$test.parameter(\"n\", \"1\"),$test.parameter(\"m\", \"7\")])");
            Assert.AreEqual("success:n=1,m=7", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallMethodWithParamsArray() {
            IScript script = parser.Parse("$test.methodwitharrayparameters(\"success\", $test.parameter(\"n\", \"1\"),$test.parameter(\"m\", \"7\"))");
            Assert.AreEqual("success:n=1,m=7", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallParamsArrayWithoutArguments() {
            IScript script = parser.Parse("$test.methodwitharrayparameters(\"success\")");
            Assert.AreEqual("success:", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallMethodWithDefaultParameters() {
            IScript script = parser.Parse("$test.methodwithdefaults(\"max\")");
            Assert.AreEqual(" max ", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void ConvertGuidToStringOnCall() {
            IScript script = parser.Parse("$test.methodwithdefaults($guid)");
            Assert.AreEqual($" {Guid.Empty} ", script.Execute(new VariableProvider(new Variable("test", this), new Variable("guid", Guid.Empty))));
        }

        [Test, Parallelizable]
        public void CallMethodSpecifyingDefaults() {
            IScript script = parser.Parse("$test.methodwithdefaults(\"max\", \"dr\")");
            Assert.AreEqual("dr max ", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallMethodSpecifyingDefaultsAndParams() {
            IScript script = parser.Parse("$test.methodwithdefaults(\"max\", \"dr\", \"k.\", \"möllemann\")");
            Assert.AreEqual("dr max k. möllemann", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallAmbigiousMethod() {
            IScript script = parser.Parse("$test.ambigious(50)");
            Assert.AreEqual("int", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallAmbigiousMethodWithFloat() {
            IScript script = parser.Parse("$test.ambigious(50.3)");
            Assert.AreEqual("string", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallInterfaceParameter() {
            IScript script = parser.Parse("$test.interfaceparameter($test.parameter(\"key\",\"value\"))");
            Assert.AreEqual("parameter", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallEnumParameter() {
            IScript script = parser.Parse("$test.enumparameter(\"second\")");
            Assert.AreEqual("enum", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Parallelizable]
        [TestCase("'a'")]
        [TestCase("7")]
        [TestCase("7u")]
        [TestCase("7l")]
        [TestCase("7ul")]
        [TestCase("7s")]
        [TestCase("7us")]
        public void CallAmbigiousMethodInt(string parameter) {
            IScript script = parser.Parse($"$test.ambigious({parameter})");
            Assert.AreEqual("int", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallAmbigiousMethodByteArray() {
            IScript script = parser.Parse("$test.ambigious([1,2,3,4,5])");
            Assert.AreEqual("bytearray", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallMethodWithRefParameter() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$variable=0",
                "$test.refparameter(ref($variable))",
                "return($variable)"
            ));

            Assert.AreEqual(42, script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallMethodWithOutParameter() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$variable=0",
                "$test.outparameter(ref($variable))",
                "return($variable)"
            ));

            Assert.AreEqual(42, script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallParamArrayUsingDictionaryConversion() {
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$test.methodwitharrayparameters(",
                    "  \"success\", ",
                    "  {",
                    "    \"name\": \"n\",",
                    "    \"value\": 1",
                    "  },",
                    "  {",
                    "    \"name\": \"m\",",
                    "    \"value\": 7",
                    "  }",
                    ")"));
            Assert.AreEqual("success:n=1,m=7", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallSingleParameterUsingDictionaryConversion() {
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$test.methodwithsingleparameter(",
                    "  \"success\", ",
                    "  {",
                    "    \"name\": \"n\",",
                    "    \"value\": 42",
                    "  }",
                    ")"));
            Assert.AreEqual("success:n=42", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void AutoConvertStringToGuid() {
            // 4cac6f34-ab34-48e5-bc5c-a0a23d282846

            IScript script = parser.Parse("$test.guidparameter(\"4cac6f34-ab34-48e5-bc5c-a0a23d282846\")");
            Assert.AreEqual("4cac6f34-ab34-48e5-bc5c-a0a23d282846", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void AutoConvertStringToNullableGuid() {
            // 4cac6f34-ab34-48e5-bc5c-a0a23d282846

            IScript script = parser.Parse("$test.nullableguidparameter(\"4cac6f34-ab34-48e5-bc5c-a0a23d282846\")");
            Assert.AreEqual("4cac6f34-ab34-48e5-bc5c-a0a23d282846", script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public void CallNullableMixedWithDefaults() {
            // 4cac6f34-ab34-48e5-bc5c-a0a23d282846

            IScript script = parser.Parse("$test.randomlistmethod(false, \"4cac6f34-ab34-48e5-bc5c-a0a23d282846\", null, [1,2])");
            Assert.DoesNotThrow(() => script.Execute(new VariableProvider(new Variable("test", this))));
        }

        [Test, Parallelizable]
        public async Task ParameterUsingImplicitOperator() {
            XElement element = new("root", new XElement("child"));
            IScript script = await parser.ParseAsync("$child=$node.element(\"child\")\nreturn($child.name)");
            Assert.AreEqual("child", await script.ExecuteAsync<string>(new VariableProvider(new Variable("node", element))));
        }

        [Test, Parallelizable]
        public async Task ParameterTypePriority() {
            IScriptParser mathparser=new ScriptParser();
            mathparser.Extensions.AddExtensions(typeof(Math));
            IScript roundscript = await mathparser.ParseAsync("return((3.57783).round(2))");
            Assert.AreEqual(3.58, await roundscript.ExecuteAsync<double>());
        }

        [Test, Parallelizable]
        public void CallGenericMethod() {
            IScriptParser mathparser = new ScriptParser();
            IScript script = mathparser.Parse("this.convert<string>(7.5)");
            Assert.AreEqual("7.5", script.Execute(new VariableProvider(new Variable("this", this))));
        }
        
        [Test, Parallelizable]
        public void CallGenericMethodWithTwoArguments() {
            IScriptParser mathparser = new ScriptParser();
            PointlessGenerics data = new();
            IScript script = mathparser.Parse("data.twoarguments<string, int>()");
            Assert.AreEqual(42, script.Execute(new VariableProvider(new Variable("data", data))));
        }

        [Test, Parallelizable]
        public void CallDictionaryArgument() {
            IScript script = parser.Parse("this.dictionarymethod({\"result\":7})");
            Assert.AreEqual(7, script.Execute(new VariableProvider(new Variable("this", this))));
        }
    }
}
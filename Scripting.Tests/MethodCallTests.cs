using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class MethodCallTests {
        readonly IScriptParser parser = new ScriptParser();

        public string MethodWithArrayParameters(string prefix, params Parameter[] parameters)
        {
            return $"{prefix}:{string.Join<Parameter>(",", parameters)}";
        }

        public string MethodWithDefaults(string name, string title = null, params string[] additional) {
            return $"{title} {name} {string.Join(" ", additional)}";
        }

        public string Ambigious(string parameter) {
            return "string";
        }

        public string Ambigious(int parameter) {
            return "int";
        }

        public string InterfaceParameter(IParameter parameter)
        {
            return "parameter";
        }

        public string EnumParameter(TestEnum @enum) {
            return "enum";
        }

        public Parameter Parameter(string name, string value) {
            return new Parameter {
                Name = name,
                Value = value
            };
        }

        [Test, Parallelizable]
        public void CallMethodWithArrayParameters() {
            IScript script = parser.Parse("$test.methodwitharrayparameters(\"success\", [$test.parameter(\"n\", \"1\"),$test.parameter(\"m\", \"7\")])", new Variable("test", this));
            Assert.AreEqual("success:n=1,m=7", script.Execute());
        }

        [Test, Parallelizable]
        public void CallMethodWithParamsArray()
        {
            IScript script = parser.Parse("$test.methodwitharrayparameters(\"success\", $test.parameter(\"n\", \"1\"),$test.parameter(\"m\", \"7\"))", new Variable("test", this));
            Assert.AreEqual("success:n=1,m=7", script.Execute());
        }

        [Test, Parallelizable]
        public void CallParamsArrayWithoutArguments() {
            IScript script = parser.Parse("$test.methodwitharrayparameters(\"success\")", new Variable("test", this));
            Assert.AreEqual("success:", script.Execute());
        }

        [Test, Parallelizable]
        public void CallMethodWithDefaultParameters() {
            IScript script = parser.Parse("$test.methodwithdefaults(\"max\")", new Variable("test", this));
            Assert.AreEqual(" max ", script.Execute());
        }

        [Test, Parallelizable]
        public void CallMethodSpecifyingDefaults() {
            IScript script = parser.Parse("$test.methodwithdefaults(\"max\", \"dr\")", new Variable("test", this));
            Assert.AreEqual("dr max ", script.Execute());
        }

        [Test, Parallelizable]
        public void CallMethodSpecifyingDefaultsAndParams() {
            IScript script = parser.Parse("$test.methodwithdefaults(\"max\", \"dr\", \"k.\", \"möllemann\")", new Variable("test", this));
            Assert.AreEqual("dr max k. möllemann", script.Execute());
        }

        [Test, Parallelizable]
        public void CallAmbigiousMethod() {
            IScript script = parser.Parse("$test.ambigious(50)", new Variable("test", this));
            Assert.AreEqual("int", script.Execute());
        }

        [Test, Parallelizable]
        public void CallAmbigiousMethodWithFloat()
        {
            IScript script = parser.Parse("$test.ambigious(50.3)", new Variable("test", this));
            Assert.AreEqual("string", script.Execute());
        }

        [Test, Parallelizable]
        public void CallInterfaceParameter()
        {
            IScript script = parser.Parse("$test.interfaceparameter($test.parameter(\"key\",\"value\"))", new Variable("test", this));
            Assert.AreEqual("parameter", script.Execute());
        }

        [Test, Parallelizable]
        public void CallEnumParameter()
        {
            IScript script = parser.Parse("$test.enumparameter(\"second\")", new Variable("test", this));
            Assert.AreEqual("enum", script.Execute());
        }

        [Test, Parallelizable]
        [TestCase("'a'")]
        [TestCase("7")]
        [TestCase("7u")]
        [TestCase("7l")]
        [TestCase("7ul")]
        [TestCase("7s")]
        [TestCase("7us")]
        public void CallAmbigiousMethodInt(string parameter)
        {
            IScript script = parser.Parse($"$test.ambigious({parameter})", new Variable("test", this));
            Assert.AreEqual("int", script.Execute());
        }
    }
}
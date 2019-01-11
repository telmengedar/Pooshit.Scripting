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
    }
}
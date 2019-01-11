using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class MethodCallTests {
        readonly IScriptParser parser = new ScriptParser();

        public string MethodWithArrayParameters(params string[] parameters) {
            return "success:"+string.Join(",", parameters);
        }

        public string Parameter(string value) {
            return value;
        }

        [Test, Parallelizable]
        public void CallMethodWithArrayParameters() {
            IScript script = parser.Parse("$test.methodwitharrayparameters([$test.parameter(\"1\"),$test.parameter(\"7\")])", new Variable("test", this));
            Assert.AreEqual("success:1,7", script.Execute());
        }

        [Test, Parallelizable]
        public void CallMethodWithParamsArray()
        {
            IScript script = parser.Parse("$test.methodwitharrayparameters($test.parameter(\"1\"),$test.parameter(\"7\"))", new Variable("test", this));
            Assert.AreEqual("success:1,7", script.Execute());
        }

        [Test, Parallelizable]
        public void CallParamsArrayWithoutArguments() {
            IScript script = parser.Parse("$test.methodwitharrayparameters()", new Variable("test", this));
            Assert.AreEqual("success:", script.Execute());
        }
    }
}
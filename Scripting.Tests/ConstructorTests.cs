using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ConstructorTests {
        readonly IScriptParser parser = new ScriptParser();

        [SetUp]
        public void Setup() {
            parser.Types.AddType<Variable>("variable");
            parser.Types.AddType<FormData>();
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

        [Test, Parallelizable]
        public void UseCorrectConstructor() {
            IScript script = parser.Parse(
                "return(new formdata($input, \"files[]\", \"test\"))"
            );
            FormData data = script.Execute<FormData>(new Dictionary<string, object>() {
                ["input"] = new MemoryStream()
            });
            Assert.That(data.Content is StreamContent);
        }
    }
}
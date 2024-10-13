using System.Linq;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Hosts;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TypeHostTests {
        readonly ScriptParser parser = new ScriptParser();

        [OneTimeSetUp]
        public void Setup() {
            parser.Types.AddType<ComplexType>("ComplexType");
        }

        [Test, Parallelizable]
        public void CreateType() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$instance=$type.create(\"ComplexType\", {",
                "  \"Parameter\": {",
                "    \"Name\":\"Name\",",
                "    \"Value\":\"Test\"",
                "  },",
                "  \"Count\":23,",
                "  \"Numbers\":[1,4,9,14]",
                "})"
            ));

            ComplexType result = script.Execute(new VariableProvider(new Variable("type", new TypeHost(parser.Types)))) as ComplexType;
            Assert.NotNull(result);
            Assert.AreEqual("Name", result.Parameter.Name);
            Assert.AreEqual("Test", result.Parameter.Value);
            Assert.AreEqual(23, result.Count);
            Assert.NotNull(result.Numbers);
            Assert.That(new[] { 1, 4, 9, 14 }.SequenceEqual(result.Numbers));
        }
    }
}
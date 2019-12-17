﻿using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Hosts;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TypeHostTests {
        readonly ScriptParser parser = new ScriptParser();

        [OneTimeSetUp]
        public void Setup() {
            parser.Types.AddType<ComplexType>("ComplexType");
            parser.GlobalVariables["type"] = new TypeHost(parser.Types);
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

            ComplexType result = script.Execute() as ComplexType;
            Assert.NotNull(result);
            Assert.AreEqual("Name", result.Parameter.Name);
            Assert.AreEqual("Test", result.Parameter.Value);
            Assert.AreEqual(23, result.Count);
            Assert.NotNull(result.Numbers);
            Assert.That(new[] {1, 4, 9, 14}.SequenceEqual(result.Numbers));
        }
    }
}
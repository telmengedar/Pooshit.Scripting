﻿using System.Collections.Generic;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests
{
    [TestFixture, Parallelizable]
    public class StringOperationTests {
        readonly IScriptParser parser = new ScriptParser();

        [TestCase("\"test\"-\"_1\"")]
        [TestCase("\"test\"/\"_1\"")]
        [TestCase("\"test\"*\"_1\"")]
        [TestCase("\"test\"%\"_1\"")]
        [TestCase("\"test\">>\"_1\"")]
        [TestCase("\"test\"<<\"_1\"")]
        [TestCase("\"test\"&\"_1\"")]
        [TestCase("\"test\"|\"_1\"")]
        [TestCase("\"test\"^\"_1\"")]
        [Parallelizable]
        public void InvalidStringOperations(string data)
        {
            IScript script = parser.Parse(data);
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, Parallelizable]
        public void Concatenate()
        {
            IScript script = parser.Parse("\"test\"+\"_1\"");
            Assert.AreEqual("test_1", script.Execute());
        }

        [Test, Parallelizable]
        public void LogicAnd() {
            Assert.AreEqual(true, parser.Parse("\"test\"&&\"wrong\"").Execute());
        }

        [Test, Parallelizable]
        public void LogicOr()
        {
            Assert.AreEqual(false, parser.Parse("\"\"&&null").Execute());
        }

        [Test, Parallelizable]
        public void LogicXor()
        {
            Assert.AreEqual(false, parser.Parse("\"test\"^^\"test7\"").Execute());
        }

        [Test, Parallelizable]
        public void PlusAssign() {
            IScript script = parser.Parse(
                "$value=\"\"\n" +
                "$value+=\"bla\""
            );

            Assert.AreEqual("bla", script.Execute());
        }

        [Test, Parallelizable]
        public void Interpolation() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value=320",
                "$\"Like {$value} reasons to hate it.\""
            ));

            Assert.AreEqual("Like 320 reasons to hate it.", script.Execute());
        }

        [Test, Parallelizable]
        public void EscapeBracketInInterpolation() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value=320",
                "$\"Use {{} to {\"interpolate\"}.\""
            ));

            Assert.AreEqual("Use {} to interpolate.", script.Execute());
        }

        [Test, Parallelizable]
        public void MultipleInterpolations() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value=320",
                "$\"There are {7+4} more ways to {\"do it\"} but lets keep it at that.\""
            ));

            Assert.AreEqual("There are 11 more ways to do it but lets keep it at that.", script.Execute());
        }

        [Test, Parallelizable]
        public void InterpolationFormatting() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value=0.107228493",
                "$\"Value: {$value:F2}\""
            ));

            Assert.AreEqual("Value: 0.11", script.Execute());
        }
        
        [Test, Parallelizable]
        public void DictionaryInterpolationFormatting() {
            IScript script = parser.Parse(ScriptCode.Create(
                "{",
                "    1: 0.3233d:F2",
                "}"
            ));

            IDictionary<object, object> result = script.Execute<IDictionary<object, object>>();
            Assert.NotNull(result);
            Assert.That(result.ContainsKey(1));
            Assert.AreEqual("0.32", result[1]);
        }

    }
}
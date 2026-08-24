using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// asserts every prefix of a set of realistic scripts returns from <see cref="ScriptParser.Parse(string)"/>
    /// within a bounded sweep
    /// </summary>
    [TestFixture, Parallelizable]
    public class TruncationPrefixFuzzTests {
        static readonly TimeSpan SweepBound = TimeSpan.FromSeconds(2);

        static string LoadResource(string name) {
            using StreamReader reader = new(typeof(TruncationPrefixFuzzTests).Assembly.GetManifestResourceStream($"Scripting.Tests.Scripts.{name}")!);
            return reader.ReadToEnd();
        }

        static IEnumerable<TestCaseData> Sources() {
            yield return new TestCaseData(LoadResource("Formatting.lukas_code.ns")).SetName("Sample_LukasCode");
            yield return new TestCaseData(LoadResource("Formatting.test2_dictionaries_input.ns")).SetName("Sample_DictionariesInput");
            yield return new TestCaseData(LoadResource("Valid.parentbug.ns")).SetName("Sample_ParentBug");

            yield return new TestCaseData(
                "$onUpdate = $delta => {\n" +
                "  $state = $state + $delta\n" +
                "  return($state)\n" +
                "}\n" +
                "{\n" +
                "  \"onUpdate\": $onUpdate,\n" +
                "  \"onStart\": $delta => { return(0) }\n" +
                "}"
            ).SetName("Template_LambdaAndHandlerMap");

            yield return new TestCaseData(
                "$options = {\n" +
                "  \"headers\": [{ \"key\": \"Authorization\", \"value\": \"Bearer \" + $token }],\n" +
                "  \"retries\": [1, 2, 3],\n" +
                "  \"nested\": { \"inner\": { \"value\": 70 } }\n" +
                "}\n" +
                "return($options)"
            ).SetName("Template_NestedArraysAndDictionaries");
        }

        [TestCaseSource(nameof(Sources))]
        public void EveryPrefix_ReturnsWithinBound(string source) {
            ScriptParser parser = new();

            bool completed = BoundedSweep.TrySweep(parser, source, SweepBound, out int stuckAt);

            Assert.That(completed, Is.True, $"parser did not return within {SweepBound} for the whole prefix sweep - stuck at prefix length {stuckAt}");
        }
    }
}

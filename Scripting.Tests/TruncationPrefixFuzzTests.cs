using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// truncation-prefix fuzzing (DiVoid #9341 §7 guard 2): for a set of realistic scripts, asserts that
    /// every prefix <c>source[0..n]</c> returns from <see cref="ScriptParser.Parse(string)"/> within a
    /// bound - it does not matter whether the prefix parses successfully or throws a
    /// <see cref="Pooshit.Scripting.Errors.ScriptParserException"/>, only that it returns at all. This is
    /// the one mechanism the diagnosis identified as catching the whole family of parse-time hangs
    /// (DiVoid #7869, #9341, and any future member) from a single test shape, rather than pinning down one
    /// specific construct the way <see cref="UnterminatedDictionaryTests"/> does. Every source used here is
    /// either an existing sample script already shipped in this test suite (loaded exactly the way
    /// <c>FormatterTests</c> loads them) or a realistic template built from constructs this repo's own test
    /// suite exercises elsewhere (lambdas, top-level dictionaries, nested arrays/dictionaries) - deliberately
    /// not tied to any one host's authoring convention.
    /// </summary>
    [TestFixture, Parallelizable]
    public class TruncationPrefixFuzzTests {
        static readonly TimeSpan Bound = TimeSpan.FromMilliseconds(500);

        static string LoadResource(string name) {
            using StreamReader reader = new(typeof(TruncationPrefixFuzzTests).Assembly.GetManifestResourceStream($"Scripting.Tests.Scripts.{name}")!);
            return reader.ReadToEnd();
        }

        static IEnumerable<TestCaseData> Sources() {
            yield return new TestCaseData(LoadResource("Formatting.lukas_code.ns")).SetName("Sample_LukasCode");
            yield return new TestCaseData(LoadResource("Formatting.test2_dictionaries_input.ns")).SetName("Sample_DictionariesInput");
            yield return new TestCaseData(LoadResource("Valid.parentbug.ns")).SetName("Sample_ParentBug");

            // realistic template: a lambda assigned to a variable, closed by the mandatory top-level
            // handler-map dictionary every "callback map" style script ends with - the exact shape DiVoid
            // #9341 hung on
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

            // realistic template: nested arrays and dictionaries, mirroring DictionaryTests.DictionaryInArray
            // and DictionaryAsComplexArgument
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
            List<int> stuck = new();

            for (int n = 0; n <= source.Length; ++n) {
                string prefix = source[..n];
                if (!BoundedParse.TryParse(parser, prefix, Bound, out _, out _))
                    stuck.Add(n);
            }

            Assert.That(stuck, Is.Empty, $"parser did not return within {Bound} for prefix length(s): {string.Join(", ", stuck)} - DiVoid #9341 family");
        }
    }
}

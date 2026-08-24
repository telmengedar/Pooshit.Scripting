using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Scripting.Tests {

    /// <summary>
    /// realistic script sources shared by the prefix-truncation and character-deletion fuzz sweeps
    /// </summary>
    static class FuzzSources {

        static string LoadResource(string name) {
            using StreamReader reader = new(typeof(FuzzSources).Assembly.GetManifestResourceStream($"Scripting.Tests.Scripts.{name}")!);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// realistic script sources used as the basis for both fuzz sweeps
        /// </summary>
        public static IEnumerable<TestCaseData> Sources() {
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
    }
}

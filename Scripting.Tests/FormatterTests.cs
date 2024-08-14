using System;
using System.IO;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Formatters;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class FormatterTests {
        readonly ScriptParser parser = new ScriptParser {
            // metatokens should be enabled for formatting to include them
            MetatokensEnabled = true
        };
        readonly ScriptFormatter formatter = new ScriptFormatter();

        [TestCase("test1_input.ns", "test1_expected.ns")]
        [TestCase("test2_dictionaries_input.ns", "test2_dictionaries_expected.ns")]
        [TestCase("test3_methods_input.ns", "test3_methods_expected.ns")]
        [TestCase("test4_indexer_input.ns", "test4_indexer_expected.ns")]
        [TestCase("test5_interpolation_input.ns", "test5_interpolation_expected.ns")]
        [TestCase("test6_comments_input.ns", "test6_comments_expected.ns")]
        [TestCase("using_code.ns", "using_expected.ns")]
        [TestCase("arithmetic_code.ns", "arithmetic_expected.ns")]
        [TestCase("subblock_code.ns", "subblock_expected.ns")]
        [TestCase("new_code.ns", "new_expected.ns")]
        [TestCase("switch_code.ns", "switch_expected.ns")]
        [TestCase("newline_code.ns", "newline_expected.ns")]
        [TestCase("eol_comments_code.ns", "eol_comments_expected.ns")]
        [TestCase("throw_code.ns", "throw_expected.ns")]
        [TestCase("cast_code.ns", "cast_expected.ns")]
        [TestCase("multilineinblock_code.ns", "multilineinblock_expected.ns")]
        [TestCase("simpleifelse_code.ns", "simpleifelse_expected.ns")]
        [TestCase("lukas_code.ns", "lukas_expected.ns")]
        [TestCase("commentinnewline_code.ns", "commentinnewline_expected.ns")]
        public void FormatTestScripts(string inputresource, string expectedresource) {
            string inputcode;
            string expectedcode;
            using (StreamReader reader = new StreamReader(typeof(FormatterTests).Assembly.GetManifestResourceStream($"Scripting.Tests.Scripts.Formatting.{inputresource}")))
                inputcode = reader.ReadToEnd();
            using (StreamReader reader = new StreamReader(typeof(FormatterTests).Assembly.GetManifestResourceStream($"Scripting.Tests.Scripts.Formatting.{expectedresource}")))
                expectedcode = reader.ReadToEnd().Replace("\r\n", "\n");

            IScript script = parser.Parse(inputcode);
            string resultcode = formatter.FormatScript(script).Replace("\r\n", "\n");
            Console.WriteLine(resultcode);
            Assert.AreEqual(expectedcode, resultcode);
        }
    }
}
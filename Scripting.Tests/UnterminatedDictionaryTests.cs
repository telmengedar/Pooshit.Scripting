using System;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptParser.ParseDictionary"/> against unterminated and malformed input
    /// </summary>
    [TestFixture, Parallelizable]
    public class UnterminatedDictionaryTests {
        static readonly TimeSpan Bound = TimeSpan.FromSeconds(3);

        [Test, Parallelizable]
        [Description("A lone top-level '{' with nothing after it must throw as unterminated, not hang.")]
        public void OpenBraceOnly_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("A key and ':' with no value and no closing brace must throw as unterminated, not hang.")]
        public void OpenMapWithKeyAndColon_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{ \"a\": ", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("A completed entry followed by a trailing comma and nothing else must throw as unterminated, not hang.")]
        public void TrailingCommaThenEof_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{ \"a\": 1,", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("A lambda plus the mandatory top-level handler map, minus its closing brace, must throw as unterminated, not hang.")]
        public void HandlerMapMinusFinalBrace_ThrowsWithinBound() {
            const string source = """
                $onUpdate = $delta => { return($delta) }
                {
                  "onUpdate": $onUpdate
                """;

            bool completed = BoundedParse.TryParse(new ScriptParser(), source, Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("Dropping a lambda body's own '}' must still throw via the bounded ParseStatementBlock path.")]
        public void LambdaBodyMinusItsBrace_StillThrowsUnterminatedStatementblock() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "$onUpdate = $delta => { self.setstate(1)", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated Statementblock"));
        }

        [Test, Parallelizable]
        [Description("A well-formed top-level dictionary must keep parsing normally.")]
        public void TerminatedDictionary_ParsesNormally() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{ \"a\": 1 }", Bound, out IScript script, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(script, Is.Not.Null);
        }

        [Test, Parallelizable]
        [Description("A trailing comma immediately followed by a closing brace is valid and must still parse.")]
        public void TerminatedDictionaryWithTrailingComma_ParsesNormally() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{ \"a\": 1, }", Bound, out IScript script, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(script, Is.Not.Null);
        }

        [Test, Parallelizable]
        [Description("A stray comma before any key must throw as malformed, not hang or report unterminated.")]
        public void LeadingStrayComma_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{,", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Malformed dictionary"));
        }

        [Test, Parallelizable]
        [Description("A stray comma between entries must throw as malformed, not hang or report unterminated.")]
        public void DoubleCommaBetweenEntries_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{ \"a\": 1, , \"b\": 2 }", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Malformed dictionary"));
        }

        [Test, Parallelizable]
        [Description("A ':' where a key is expected must throw as malformed even though the map is closed - not reported as unterminated.")]
        public void ColonWithoutKey_ThrowsWithinBoundAndIsNotReportedUnterminated() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "{ : }", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Malformed dictionary"));
            Assert.That(error.Message, Does.Not.Contain("Unterminated"));
        }
    }
}

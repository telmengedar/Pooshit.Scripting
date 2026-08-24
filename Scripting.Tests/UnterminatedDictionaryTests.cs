using System;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptParser.ParseDictionary"/> against unterminated input (DiVoid #9341, filed
    /// from Uberkarl #9070: "as soon as i remove the last '}' from the script the whole playtest freezes
    /// and i have to force-kill it"). Both of <c>ParseDictionary</c>'s loops compared <c>Peek(data, index)</c>
    /// against '}' with no <c>index &lt; data.Length</c> bound; <c>Peek</c> returns '\0' at end of input,
    /// which is never '}', and a failed <c>Parse</c> call on exhausted input returns <c>null</c> without
    /// advancing <c>index</c> - so the inner loop called <c>Parse</c> forever. Every test here goes through
    /// <see cref="BoundedParse"/> rather than a bare <c>Assert.Throws</c>, specifically so that a regression
    /// reintroducing the unbounded loop fails the assertion instead of hanging the whole test run - the gap
    /// the diagnosis identified as the reason 893 existing tests and an 8/8 headless probe missed this in
    /// the first place (no guard in the suite asserted termination).
    /// </summary>
    [TestFixture, Parallelizable]
    public class UnterminatedDictionaryTests {
        static readonly TimeSpan Bound = TimeSpan.FromSeconds(3);

        readonly ScriptParser parser = new();

        [Test, Parallelizable]
        [Description("DiVoid #9341 minimal reproducer: a lone top-level '{' with nothing after it.")]
        public void OpenBraceOnly_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(parser, "{", Bound, out _, out Exception error);

            Assert.That(completed, Is.True, "parser did not return within the bound - DiVoid #9341 (ParseDictionary spinning at EOF)");
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("DiVoid #9341 'open-map' case: a key present, ':' present, but no value and no closing brace.")]
        public void OpenMapWithKeyAndColon_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(parser, "{ \"a\": ", Bound, out _, out Exception error);

            Assert.That(completed, Is.True, "parser did not return within the bound - DiVoid #9341 (ParseDictionary spinning at EOF)");
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("DiVoid #9341: a completed entry followed by a trailing comma and nothing else - the inner (key-retry) loop is what spins here.")]
        public void TrailingCommaThenEof_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(parser, "{ \"a\": 1,", Bound, out _, out Exception error);

            Assert.That(completed, Is.True, "parser did not return within the bound - DiVoid #9341 (ParseDictionary spinning at EOF)");
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("DiVoid #9341 realistic shape: a lambda assigned to a variable followed by the mandatory top-level handler-map dictionary, minus its closing brace - this is exactly the modal typo the diagnosis identified, since a top-level '{' (not a lambda body '=>{') is the construct every author's script ends with.")]
        public void HandlerMapMinusFinalBrace_ThrowsWithinBound() {
            const string source = """
                $onUpdate = $delta => { return($delta) }
                {
                  "onUpdate": $onUpdate
                """;

            bool completed = BoundedParse.TryParse(parser, source, Bound, out _, out Exception error);

            Assert.That(completed, Is.True, "parser did not return within the bound - DiVoid #9341 (ParseDictionary spinning at EOF)");
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("Control case from the diagnosis: dropping the lambda body's own '}' (not the top-level dictionary's) already threw cleanly before this fix, via the bounded ParseStatementBlock path - must keep doing so.")]
        public void LambdaBodyMinusItsBrace_StillThrowsUnterminatedStatementblock() {
            bool completed = BoundedParse.TryParse(parser, "$onUpdate = $delta => { self.setstate(1)", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated Statementblock"));
        }

        [Test, Parallelizable]
        [Description("A well-formed top-level dictionary must keep parsing normally - the EOF guard must not reject terminated input.")]
        public void TerminatedDictionary_ParsesNormally() {
            bool completed = BoundedParse.TryParse(parser, "{ \"a\": 1 }", Bound, out IScript script, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(script, Is.Not.Null);
        }

        [Test, Parallelizable]
        [Description("A trailing comma immediately followed by a closing brace is valid and must still parse - the EOF guard must not disturb this existing behavior.")]
        public void TerminatedDictionaryWithTrailingComma_ParsesNormally() {
            bool completed = BoundedParse.TryParse(parser, "{ \"a\": 1, }", Bound, out IScript script, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(script, Is.Not.Null);
        }
    }
}

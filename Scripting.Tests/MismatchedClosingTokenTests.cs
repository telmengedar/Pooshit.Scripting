using System;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises the tightened grammar after <c>ParseParameters</c> folded into <see cref="ScriptParser.ParseTokenList"/>:
    /// <c>)</c> and <c>]</c> are no longer interchangeable closers
    /// </summary>
    [TestFixture, Parallelizable]
    public class MismatchedClosingTokenTests {
        static readonly TimeSpan Bound = TimeSpan.FromSeconds(3);

        [Test, Parallelizable]
        [Description("A ')' closing an indexer where ']' is expected must be rejected by the pre-dispatch closer check, not silently accepted.")]
        public void CloseParenInIndexer_ThrowsMismatchedClosingToken() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "$a[1)", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Mismatched closing token ')' in parameter list, expected ']'."));
        }

        [Test, Parallelizable]
        [Description("A ']' closing a control statement's parameter list where ')' is expected must still be rejected via the progress guard, naming the offending character - not intercepted by the ')'-only pre-dispatch check.")]
        public void CloseBracketInControlParameters_ThrowsUnexpectedTokenNamingCharacter() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "if(1]$x=1", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unexpected token ']' in parameter list, expected ')'."));
        }

        [Test, Parallelizable]
        [Description("A well-formed indexer must still parse normally after the fold.")]
        public void WellFormedIndexer_ParsesNormally() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "$a[1]", Bound, out IScript script, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(script, Is.Not.Null);
        }

        [Test, Parallelizable]
        [Description("A well-formed control statement's parameter list must still parse normally after the fold.")]
        public void WellFormedControlParameters_ParseNormally() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "if(1){$x=1}", Bound, out IScript script, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(script, Is.Not.Null);
        }
    }
}

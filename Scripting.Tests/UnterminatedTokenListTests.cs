using System;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptParser.ParseTokenList"/>'s progress guard against a stray terminator where a parameter is expected
    /// </summary>
    [TestFixture, Parallelizable]
    public class UnterminatedTokenListTests {
        static readonly TimeSpan Bound = TimeSpan.FromSeconds(3);

        [Test, Parallelizable]
        [Description("A stray ']' where a call argument is expected must throw within bound instead of hanging.")]
        public void StrayClosingBracketInCallArguments_ThrowsWithinBound() {
            bool completed = BoundedParse.TryParse(new ScriptParser(), "$x = foo(]", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unexpected token ']' in parameter list, expected ')'."));
        }
    }
}

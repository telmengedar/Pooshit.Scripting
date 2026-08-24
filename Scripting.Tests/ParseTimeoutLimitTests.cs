using System;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptLimits.ParseTimeout"/> (DiVoid #9341 defence-in-depth): a parse-time
    /// wall-clock ceiling checked on every recursive descent through <c>ScriptParser.Parse</c>, alongside
    /// the existing <see cref="ScriptLimits.MaxParseDepth"/> (DiVoid #7869). <see cref="MaxParseDepth"/>
    /// bounds recursion depth; it structurally cannot catch a construct that loops back into the parser at
    /// a constant depth (exactly what DiVoid #9341's unterminated dictionary did before it was given its own
    /// EOF guard - see <see cref="UnterminatedDictionaryTests"/>). <see cref="ScriptLimits.ParseTimeout"/> is
    /// the family-wide backstop: it does not care which construct is looping, only that the parser has been
    /// re-entered past the configured deadline.
    /// </summary>
    [TestFixture, Parallelizable]
    public class ParseTimeoutLimitTests {
        static readonly TimeSpan Bound = TimeSpan.FromSeconds(3);

        [Test, Parallelizable]
        public void Default_ParseTimeoutIsFiveSeconds() {
            Assert.That(ScriptLimits.DefaultParseTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(ScriptLimits.Default.ParseTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test, Parallelizable]
        [Description("A zero timeout has already elapsed by the time the first recursive Parse call is reached, so even trivial, well-formed input must be refused with a catchable ScriptParserException - proves the deadline is actually consulted, without needing a real non-terminating construct to trigger it.")]
        public void ZeroTimeout_RefusesEvenTrivialInputWithScriptParserException() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.Zero}
            };

            bool completed = BoundedParse.TryParse(parser, "$x = 1 + 2", Bound, out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("parse timeout"));
        }

        [Test, Parallelizable]
        [Description("The message reports the configured timeout value, matching the wording style of the MaxParseDepth message ('...exceeded the configured nesting depth limit of...').")]
        public void ZeroTimeout_MessageReportsConfiguredValue() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.Zero}
            };

            ScriptParserException exception = Assert.Throws<ScriptParserException>(() => parser.Parse("$x = 1"));
            Assert.That(exception.Message, Does.Contain("Parser exceeded the configured parse timeout"));
        }

        [Test, Parallelizable]
        [Description("An ordinary script well within a generous timeout must keep parsing and executing normally - the guard must not disturb legitimate input.")]
        public void GenerousTimeout_ParsesAndExecutesNormally() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.FromSeconds(30)}
            };

            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse("$x = 1 + 2\nreturn($x)"));
            Assert.That(script.Execute(), Is.EqualTo(3));
        }

        [Test, Parallelizable]
        [Description("ScriptLimits.None must opt out of the parse-timeout ceiling entirely, exactly like every other knob.")]
        public void NoneLimits_OptsOutOfParseTimeout() {
            ScriptParser parser = new() {
                Limits = ScriptLimits.None
            };

            Assert.That(parser.Limits.ParseTimeout, Is.Null);
            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse("$x = 1 + 2\nreturn($x)"));
            Assert.That(script.Execute(), Is.EqualTo(3));
        }

        [Test, Parallelizable]
        [Description("A realistic script must still parse and execute under the actual production default (ScriptLimits.Default) - the deadline exists to stop a non-terminating parse, not ordinary scripts.")]
        public void ProductionDefault_ParsesRealisticScriptNormally() {
            ScriptParser parser = new();

            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse(
                "$onUpdate = $delta => { return($delta) }\n" +
                "{\n" +
                "  \"onUpdate\": $onUpdate\n" +
                "}"
            ));
            Assert.That(script, Is.Not.Null);
        }

        [Test, Parallelizable]
        [Description("DiVoid #9341's own minimal reproducer, run under the production default with no per-test override, must be refused as a clean parse timeout even if some future regression reopened the EOF hole the dictionary-specific fix closes - this is the family-wide backstop working as intended.")]
        public void ProductionDefault_StillBoundsTheOriginalReproducer() {
            ScriptParser parser = new();

            bool completed = BoundedParse.TryParse(parser, "{", TimeSpan.FromSeconds(10), out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
        }
    }
}

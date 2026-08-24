using System;
using System.Text;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptLimits.ParseTimeout"/>, the parse-time wall-clock backstop
    /// </summary>
    [TestFixture, Parallelizable]
    public class ParseTimeoutLimitTests {
        static readonly TimeSpan Bound = TimeSpan.FromSeconds(3);

        static string SlowLegitimateDictionarySource(int entries) {
            StringBuilder source = new();
            source.Append('{');
            for (int i = 0; i < entries; ++i) {
                if (i > 0) source.Append(',');
                source.Append('"').Append('k').Append(i).Append("\":").Append(i);
            }
            source.Append('}');
            return source.ToString();
        }

        [Test, Parallelizable]
        public void Default_ParseTimeoutIsFiveSeconds() {
            Assert.That(ScriptLimits.DefaultParseTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(ScriptLimits.Default.ParseTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test, Parallelizable]
        [Description("A zero timeout must refuse even trivial input with a catchable ScriptParserException.")]
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
        [Description("The exception message must report the configured timeout value.")]
        public void ZeroTimeout_MessageReportsConfiguredValue() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.Zero}
            };

            ScriptParserException exception = Assert.Throws<ScriptParserException>(() => parser.Parse("$x = 1"));
            Assert.That(exception.Message, Does.Contain("Parser exceeded the configured parse timeout"));
        }

        [Test, Parallelizable]
        [Description("An ordinary script well within a generous timeout must keep parsing and executing normally.")]
        public void GenerousTimeout_ParsesAndExecutesNormally() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.FromSeconds(30)}
            };

            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse("$x = 1 + 2\nreturn($x)"));
            Assert.That(script.Execute(), Is.EqualTo(3));
        }

        [Test, Parallelizable]
        [Description("ScriptLimits.None must opt out of the parse-timeout ceiling entirely.")]
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
        [Description("A realistic script must still parse and execute under the production default.")]
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
        [Description("The production default must still refuse DiVoid #9341's own minimal reproducer.")]
        public void ProductionDefault_StillRefusesTheOriginalReproducer() {
            ScriptParser parser = new();

            bool completed = BoundedParse.TryParse(parser, "{", TimeSpan.FromSeconds(10), out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("Unterminated dictionary"));
        }

        [Test, Parallelizable]
        [Description("A short timeout must cut off a slow but legitimate, terminating parse before it completes, pinning the deadline to its configured order of magnitude rather than only its presence.")]
        public void ShortTimeout_CutsOffSlowLegitimateParseBeforeItCompletes() {
            string source = SlowLegitimateDictionarySource(50_000);
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.FromMilliseconds(5)}
            };

            bool completed = BoundedParse.TryParse(parser, source, TimeSpan.FromSeconds(5), out _, out Exception error);

            Assert.That(completed, Is.True);
            Assert.That(error, Is.InstanceOf<ScriptParserException>());
            Assert.That(error.Message, Does.Contain("parse timeout"));
        }

        [Test, Parallelizable]
        [Description("A configured timeout must not leak a permanently incremented parse-depth counter into later parses on the same thread.")]
        public void TimeoutOnThreadWithDepthLimit_DoesNotLeakDepthAcrossLaterParses() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {ParseTimeout = TimeSpan.Zero, MaxParseDepth = 100}
            };

            ScriptParserException last = null;
            for (int i = 0; i < 150; ++i)
                last = Assert.Throws<ScriptParserException>(() => parser.Parse("$x = 1"));

            Assert.That(last.Message, Does.Contain("parse timeout"));
            Assert.That(last.Message, Does.Not.Contain("nesting depth"));
        }
    }
}

using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptLimits.RegexTimeout"/> (DiVoid #7871): catastrophic backtracking inside the
    /// <c>~~</c>/<c>!~</c> operators runs inside one native <see cref="Regex.IsMatch(string,string,RegexOptions,TimeSpan)"/>
    /// call, so no step budget and no caller <see cref="System.Threading.CancellationToken"/> can interrupt it -
    /// the configured timeout is the only guard. Every backtracking payload here is sized so the unguarded
    /// (pre-fix) running time is a few seconds at most, measured directly against this repo's build, so a
    /// regression that drops the default back to <c>null</c> fails the assertion within a bounded time rather
    /// than hanging the test run
    /// </summary>
    [TestFixture, Parallelizable]
    public class RegexTimeoutTests {

        /// <summary>
        /// classic catastrophic-backtracking pattern against a non-matching input of length <paramref name="n"/> + 1;
        /// measured directly against <see cref="Regex.IsMatch(string,string)"/> on this build: unguarded running time
        /// roughly doubles per increment of <paramref name="n"/> (n=24 ~2.3s, n=25 ~7.5s, n=26 ~14.5s)
        /// </summary>
        static string BacktrackingInput(int n) => new string('a', n) + "b";

        const string BacktrackingPattern = "^(a+)+$";

        [Test, Parallelizable, MaxTime(2000)]
        public void Default_RegexTimeoutIsOneSecond() {
            Assert.That(ScriptLimits.DefaultRegexTimeout, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(ScriptLimits.Default.RegexTimeout, Is.EqualTo(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void None_LeavesRegexTimeoutUnset() {
            Assert.That(ScriptLimits.None.RegexTimeout, Is.Null);
        }

        [Test, Parallelizable, MaxTime(10000)]
        [Description("DiVoid #7871: on a bare ScriptParser (ScriptLimits.Default, no per-test override), catastrophic backtracking through '~~' throws instead of running unbounded. n=25 is sized so the unguarded running time measured directly against Regex.IsMatch on this build is ~7.5s - bounded even if this regresses, never a hang - while comfortably exceeding the 1s default so the guard is what actually stops it, not a payload too small to matter.")]
        public void Matches_CatastrophicBacktrackingUnderProductionDefaultThrows() {
            ScriptParser parser = new();
            IScript script = parser.Parse($"\"{BacktrackingInput(25)}\" ~~ \"{BacktrackingPattern}\"");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.InnerException, Is.InstanceOf<RegexMatchTimeoutException>());
        }

        [Test, Parallelizable, MaxTime(3000)]
        [Description("Mechanism pin distinct from the production-default test above: a small explicit RegexTimeout must be honored by the '~~' operator regardless of ScriptLimits.Default. n=24 keeps the unguarded worst case (~2.3s, measured) short even if the override were silently ignored.")]
        public void Matches_CatastrophicBacktrackingWithConfiguredTimeoutThrows() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {RegexTimeout = TimeSpan.FromMilliseconds(50)}
            };
            IScript script = parser.Parse($"\"{BacktrackingInput(24)}\" ~~ \"{BacktrackingPattern}\"");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.InnerException, Is.InstanceOf<RegexMatchTimeoutException>());
        }

        [Test, Parallelizable, MaxTime(3000)]
        [Description("Sweep coverage for MatchesNot.cs alongside Matches.cs - '!~' reads the exact same context.Limits.RegexTimeout expression and must be guarded identically.")]
        public void MatchesNot_CatastrophicBacktrackingWithConfiguredTimeoutThrows() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {RegexTimeout = TimeSpan.FromMilliseconds(50)}
            };
            IScript script = parser.Parse($"\"{BacktrackingInput(24)}\" !~ \"{BacktrackingPattern}\"");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.InnerException, Is.InstanceOf<RegexMatchTimeoutException>());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Acceptance counterpart: an ordinary, non-backtracking match must still succeed under the new production default - the guard must not false-positive on realistic patterns.")]
        public void Matches_OrdinaryPatternUnderProductionDefaultStillSucceeds() {
            ScriptParser parser = new();
            IScript script = parser.Parse("\"TestString\" ~~ \"^.*t.t.*$\"");

            Assert.That(script.Execute(), Is.EqualTo(true));
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void ScriptLimits_RegexTimeoutIsHostOverridableBackToUnbounded() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {RegexTimeout = Regex.InfiniteMatchTimeout}
            };

            Assert.That(parser.Limits.RegexTimeout, Is.EqualTo(Regex.InfiniteMatchTimeout));
        }
    }
}

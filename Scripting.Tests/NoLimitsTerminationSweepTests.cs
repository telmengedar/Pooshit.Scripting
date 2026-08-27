using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// asserts every swept prefix, deletion, and alphabet-corpus input returns from <see cref="ScriptParser.Parse(string)"/> within a bound, under <see cref="ScriptLimits.None"/>
    /// </summary>
    [TestFixture, Parallelizable]
    public class NoLimitsTerminationSweepTests {
        static readonly TimeSpan SweepBound = TimeSpan.FromSeconds(20);
        static readonly TimeSpan CorpusBound = TimeSpan.FromSeconds(10);

        [Test, Parallelizable]
        [TestCaseSource(typeof(FuzzSources), nameof(FuzzSources.Sources))]
        public void EveryPrefix_ReturnsWithinBound_WithNoConfiguredLimits(string source) {
            ScriptParser parser = new() {Limits = ScriptLimits.None};

            bool completed = BoundedSweep.TrySweep(parser, source, SweepBound, out int stuckAt);

            Assert.That(completed, Is.True, $"parser did not return within {SweepBound} for the whole prefix sweep under ScriptLimits.None - stuck at prefix length {stuckAt}");
        }

        [Test, Parallelizable]
        [TestCaseSource(typeof(FuzzSources), nameof(FuzzSources.Sources))]
        public void EveryDeletion_ReturnsWithinBound_WithNoConfiguredLimits(string source) {
            ScriptParser parser = new() {Limits = ScriptLimits.None};

            bool completed = BoundedSweep.TrySweepDeletions(parser, source, SweepBound, out int stuckAt);

            Assert.That(completed, Is.True, $"parser did not return within {SweepBound} for the whole deletion sweep under ScriptLimits.None - stuck deleting index {stuckAt}");
        }

        [Test, Parallelizable]
        public void EveryAlphabetCorpusInput_ReturnsWithinBound_WithNoConfiguredLimits() {
            ScriptParser parser = new() {Limits = ScriptLimits.None};
            IReadOnlyList<string> corpus = TerminationSweepCorpus.Generate();

            bool completed = BoundedSweep.TrySweepInputs(parser, corpus, CorpusBound, out int stuckAt);

            Assert.That(completed, Is.True, $"parser did not return within {CorpusBound} for the whole {corpus.Count}-input alphabet corpus under ScriptLimits.None - stuck at input {stuckAt} ({(stuckAt >= 0 ? corpus[stuckAt] : "n/a")})");
        }
    }
}

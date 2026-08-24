using System;
using NUnit.Framework;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// asserts every prefix of a set of realistic scripts returns from <see cref="ScriptParser.Parse(string)"/>
    /// within a bounded sweep
    /// </summary>
    [TestFixture, Parallelizable]
    public class TruncationPrefixFuzzTests {
        static readonly TimeSpan SweepBound = TimeSpan.FromSeconds(2);

        [Test, Parallelizable]
        [TestCaseSource(typeof(FuzzSources), nameof(FuzzSources.Sources))]
        public void EveryPrefix_ReturnsWithinBound(string source) {
            ScriptParser parser = new();

            bool completed = BoundedSweep.TrySweep(parser, source, SweepBound, out int stuckAt);

            Assert.That(completed, Is.True, $"parser did not return within {SweepBound} for the whole prefix sweep - stuck at prefix length {stuckAt}");
        }
    }
}

using System;
using NUnit.Framework;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// asserts every single-character deletion of a set of realistic scripts returns from
    /// <see cref="ScriptParser.Parse(string)"/> within a bounded sweep
    /// </summary>
    [TestFixture, Parallelizable]
    public class DeletionMutationFuzzTests {
        static readonly TimeSpan SweepBound = TimeSpan.FromSeconds(2);

        [Test, Parallelizable]
        [TestCaseSource(typeof(FuzzSources), nameof(FuzzSources.Sources))]
        public void EveryDeletion_ReturnsWithinBound(string source) {
            ScriptParser parser = new();

            bool completed = BoundedSweep.TrySweepDeletions(parser, source, SweepBound, out int stuckAt);

            Assert.That(completed, Is.True, $"parser did not return within {SweepBound} for the whole deletion sweep - stuck deleting index {stuckAt}");
        }
    }
}

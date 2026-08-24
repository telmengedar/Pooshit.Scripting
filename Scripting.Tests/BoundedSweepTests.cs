using System;
using NUnit.Framework;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="BoundedSweep"/> against a fake parser with a controllable hang, independent of
    /// any real parser defect
    /// </summary>
    [TestFixture, Parallelizable]
    public class BoundedSweepTests {
        static readonly TimeSpan Bound = TimeSpan.FromMilliseconds(300);

        [Test, Parallelizable]
        public void TrySweep_ReturnsTrueAndMinusOne_WhenEveryPrefixReturns() {
            ScriptedParser parser = new();

            bool completed = BoundedSweep.TrySweep(parser, "abcde", Bound, out int stuckAt);

            Assert.That(completed, Is.True);
            Assert.That(stuckAt, Is.EqualTo(-1));
        }

        [Test, Parallelizable]
        public void TrySweep_ReturnsFalseAndNamesTheHungPrefix_WhenAPrefixNeverReturns() {
            ScriptedParser parser = new() {HangAt = 3};

            bool completed = BoundedSweep.TrySweep(parser, "abcde", Bound, out int stuckAt);

            Assert.That(completed, Is.False);
            Assert.That(stuckAt, Is.EqualTo(3));
        }

        [Test, Parallelizable]
        public void TrySweep_KeepsSweepingPastAThrownException_AndStillFindsALaterHang() {
            ScriptedParser parser = new() {ThrowAt = 1, HangAt = 3};

            bool completed = BoundedSweep.TrySweep(parser, "abcde", Bound, out int stuckAt);

            Assert.That(completed, Is.False);
            Assert.That(stuckAt, Is.EqualTo(3));
        }

        [Test, Parallelizable]
        [Description("The last single-character deletion must be part of the swept variant set, not dropped by an off-by-one.")]
        public void TrySweepDeletions_ReachesTheLastDeletion() {
            ScriptedParser parser = new() {HangOnData = "abcd"};

            bool completed = BoundedSweep.TrySweepDeletions(parser, "abcde", Bound, out int stuckAt);

            Assert.That(completed, Is.False);
            Assert.That(stuckAt, Is.EqualTo(4));
        }
    }
}

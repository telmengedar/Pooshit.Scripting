using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Expressions;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Providers;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="BoundedSweep"/> against a fake parser with a controllable hang, independent of
    /// any real parser defect
    /// </summary>
    [TestFixture, Parallelizable]
    public class BoundedSweepTests {
        static readonly TimeSpan Bound = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// fake parser that throws on the prefix length <see cref="ThrowAt"/> and hangs on the prefix length
        /// <see cref="HangAt"/>, otherwise returns immediately
        /// </summary>
        class ScriptedParser : IScriptParser {
            public int ThrowAt = -1;
            public int HangAt = -1;

            public IExtensionProvider Extensions => throw new NotImplementedException();
            public ITypeProvider Types => throw new NotImplementedException();
            public IImportProvider ImportProvider { get; set; }

            public IScript Parse(string data) {
                if (data.Length == ThrowAt)
                    throw new InvalidOperationException();
                if (data.Length == HangAt)
                    Thread.Sleep(Timeout.Infinite);
                return null;
            }

            public Task<IScript> ParseAsync(string data) => throw new NotImplementedException();
            public Delegate ParseDelegate(string data, params LambdaParameter[] parameters) => throw new NotImplementedException();
            public T ParseDelegate<T>(string data, params LambdaParameter[] parameters) => throw new NotImplementedException();
        }

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
    }
}

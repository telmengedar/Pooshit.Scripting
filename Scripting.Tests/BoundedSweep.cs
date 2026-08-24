using System;
using System.Threading;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// runs a parse over every prefix of a source on one dedicated background thread, bounded by a single
    /// deadline for the whole sweep
    /// </summary>
    static class BoundedSweep {

        /// <summary>
        /// parses every prefix <c>source[0..n]</c> of <paramref name="source"/>, for <c>n</c> from 0 to
        /// <c>source.Length</c>, on a dedicated background thread, waiting at most <paramref name="bound"/>
        /// for the whole sweep to finish
        /// </summary>
        /// <param name="parser">parser to use</param>
        /// <param name="source">script source whose prefixes are swept</param>
        /// <param name="bound">maximum time to wait for the whole sweep</param>
        /// <param name="stuckAt">prefix length the worker was parsing when the bound elapsed, or -1 when the sweep finished within the bound</param>
        /// <returns>true when the sweep finished within <paramref name="bound"/>; false when the bound elapsed first</returns>
        public static bool TrySweep(IScriptParser parser, string source, TimeSpan bound, out int stuckAt) {
            int current = 0;
            Thread worker = new(() => {
                for (int n = 0; n <= source.Length; ++n) {
                    Volatile.Write(ref current, n);
                    try {
                        parser.Parse(source[..n]);
                    }
                    catch {
                    }
                }
            }) {IsBackground = true};

            worker.Start();
            bool completed = worker.Join(bound);
            stuckAt = completed ? -1 : Volatile.Read(ref current);
            return completed;
        }
    }
}

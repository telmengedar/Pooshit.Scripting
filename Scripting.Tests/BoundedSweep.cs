using System;
using System.Collections.Generic;
using System.Threading;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// runs a parse over a sequence of source variants on one dedicated background thread, bounded by a
    /// single deadline for the whole sweep
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
            return TrySweepVariants(parser, source.Length + 1, n => source[..n], bound, out stuckAt);
        }

        /// <summary>
        /// parses every single-character deletion of <paramref name="source"/>, on a dedicated background
        /// thread, waiting at most <paramref name="bound"/> for the whole sweep to finish
        /// </summary>
        /// <param name="parser">parser to use</param>
        /// <param name="source">script source whose single-character deletions are swept</param>
        /// <param name="bound">maximum time to wait for the whole sweep</param>
        /// <param name="stuckAt">index of the deleted character the worker was parsing when the bound elapsed, or -1 when the sweep finished within the bound</param>
        /// <returns>true when the sweep finished within <paramref name="bound"/>; false when the bound elapsed first</returns>
        public static bool TrySweepDeletions(IScriptParser parser, string source, TimeSpan bound, out int stuckAt) {
            return TrySweepVariants(parser, source.Length, i => source.Remove(i, 1), bound, out stuckAt);
        }

        /// <summary>
        /// parses every input in <paramref name="inputs"/>, on a dedicated background thread, waiting at
        /// most <paramref name="bound"/> for the whole sweep to finish
        /// </summary>
        /// <param name="parser">parser to use</param>
        /// <param name="inputs">script source variants to sweep</param>
        /// <param name="bound">maximum time to wait for the whole sweep</param>
        /// <param name="stuckAt">index into <paramref name="inputs"/> the worker was parsing when the bound elapsed, or -1 when the sweep finished within the bound</param>
        /// <returns>true when the sweep finished within <paramref name="bound"/>; false when the bound elapsed first</returns>
        public static bool TrySweepInputs(IScriptParser parser, IReadOnlyList<string> inputs, TimeSpan bound, out int stuckAt) {
            return TrySweepVariants(parser, inputs.Count, n => inputs[n], bound, out stuckAt);
        }

        static bool TrySweepVariants(IScriptParser parser, int variantCount, Func<int, string> variant, TimeSpan bound, out int stuckAt) {
            int current = 0;
            Thread worker = new(() => {
                for (int n = 0; n < variantCount; ++n) {
                    Volatile.Write(ref current, n);
                    try {
                        parser.Parse(variant(n));
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

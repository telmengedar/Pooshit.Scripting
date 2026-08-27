using System.Collections.Generic;

namespace Scripting.Tests {

    /// <summary>
    /// generates the alphabet-cross-product input set for the standing termination sweep
    /// </summary>
    static class TerminationSweepCorpus {

        /// <summary>
        /// every character the parser dispatches on structurally, one letter, one digit, whitespace,
        /// an escape-handling case label, and characters outside the grammar
        /// </summary>
        static readonly char[] Alphabet = [
            '$', '(', ')', '[', ']', '{', '}', ',', ';', ':', '.', '=', '!', '~', '<', '>', '/', '+', '*', '-', '%', '&', '|', '^', '?', '"', '\'',
            'a', '1', ' ',
            '@', '#', '`', '\\'
        ];

        /// <summary>
        /// realistic dispatch contexts prefixed onto every 2-character alphabet suffix
        /// </summary>
        static readonly string[] Contexts = [
            "$a[", "f(", "{", "[", "{\"a\":1,", "$a[1,", "f(1,", "$a=$b<", "$a[1:", "new list<", "{\"a\":", "$a.b(", "if(", "$a=>{", "$x=1;"
        ];

        /// <summary>
        /// every 1- and 2-character string over <see cref="Alphabet"/>, plus every <see cref="Contexts"/> entry
        /// followed by every 2-character <see cref="Alphabet"/> suffix
        /// </summary>
        public static IReadOnlyList<string> Generate() {
            List<string> corpus = [];

            foreach (char a in Alphabet)
                corpus.Add(a.ToString());

            foreach (char a in Alphabet)
                foreach (char b in Alphabet)
                    corpus.Add(new string([a, b]));

            foreach (string context in Contexts)
                foreach (char a in Alphabet)
                    foreach (char b in Alphabet)
                        corpus.Add(context + a + b);

            return corpus;
        }
    }
}

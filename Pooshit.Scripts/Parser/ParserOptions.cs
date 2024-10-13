using Pooshit.Scripting.Data;

namespace Pooshit.Scripting.Parser {

    /// <summary>
    /// options for parser
    /// </summary>
    public class ParserOptions {

        /// <summary>
        /// whether to include tokens which have to effect in script
        /// </summary>
        /// <remarks>
        /// e.g. comments
        /// </remarks>
        public bool IncludeNonExecutableTokens { get; set; }

        /// <summary>
        /// variables to use when parsing script
        /// </summary>
        public Variable[] Variables { get; set; }
    }
}
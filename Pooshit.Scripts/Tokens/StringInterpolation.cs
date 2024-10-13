using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Control;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// interpolates tokens to a string
    /// </summary>
    public class StringInterpolation : ITokenContainer, IScriptToken {
        readonly IScriptToken[] tokens;

        /// <summary>
        /// creates a new <see cref="StringInterpolation"/>
        /// </summary>
        /// <param name="tokens">tokens to interpolate</param>
        public StringInterpolation(IScriptToken[] tokens) {
            this.tokens = tokens;
        }

        /// <inheritdoc />
        public string Literal => "$\"...\"";

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            return string.Join("", tokens.Select(t => t.Execute(context)?.ToString()));
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Children => tokens;
    }
}
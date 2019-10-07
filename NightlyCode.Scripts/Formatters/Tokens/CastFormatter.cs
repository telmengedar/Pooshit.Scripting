﻿using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="TypeCast"/>s
    /// </summary>
    public class CastFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            TypeCast cast = (TypeCast) token;

            resulttext.Append(cast.Keyword).Append('(');
            formatters[cast.Argument].FormatToken(cast.Argument, resulttext, formatters, depth);
            resulttext.Append(')');
        }
    }
}
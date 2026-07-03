using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens;

/// <summary>
/// formats <see cref="ConditionalAccess"/> as <c>receiver?.continuation</c>
/// </summary>
public class ConditionalAccessFormatter : TokenFormatter {

    /// <inheritdoc />
    protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
        ConditionalAccess ca = (ConditionalAccess)token;
        formatters[ca.Receiver].FormatToken(ca.Receiver, resulttext, formatters, depth);
        resulttext.Append('?');
        formatters[ca.Continuation].FormatToken(ca.Continuation, resulttext, formatters, depth);
    }
}

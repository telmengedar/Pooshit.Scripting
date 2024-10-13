using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ScriptMember"/>
    /// </summary>
    public class MemberFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ScriptMember member = (ScriptMember) token;
            formatters[member.Host].FormatToken(member.Host, resulttext, formatters, depth);
            resulttext.Append('.').Append(member.Member);
        }
    }
}
using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats a token for a <see cref="IScriptFormatter"/>
    /// </summary>
    public interface ITokenFormatter {

        /// <summary>
        /// formats a token to a result text
        /// </summary>
        /// <param name="token">token to format</param>
        /// <param name="resulttext">text where formatted string is stored</param>
        /// <param name="formatters">available formatters</param>
        /// <param name="depth">block depth in hierarchy</param>
        /// <param name="mustindent">determines whether depth is just an indicator or must be applied</param>
        void FormatToken(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0, bool mustindent=false);
    }
}
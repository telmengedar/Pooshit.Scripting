using System.Text;
using NightlyCode.Scripting.Formatters.Tokens;

namespace NightlyCode.Scripting.Formatters {

    /// <inheritdoc />
    public class ScriptFormatter : IScriptFormatter {
        readonly IFormatterCollection formatters;

        /// <summary>
        /// creates a new <see cref="ScriptFormatter"/>
        /// </summary>
        /// <param name="formatters">formatters to use when formatting tokens (optional)</param>
        public ScriptFormatter(IFormatterCollection formatters=null) {
            this.formatters = formatters ?? new DefaultFormatterCollection();
        }

        /// <inheritdoc />
        public string FormatScript(IScript script) {
            StringBuilder resulttext = new StringBuilder();
            formatters[script.Body].FormatToken(script.Body, resulttext, formatters, -1);
            return resulttext.ToString();
        }
    }
}
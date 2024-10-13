using System.Text;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for script exceptions
    /// </summary>
    public static class ScriptExceptionExtensions {

        static void Traverse(ScriptRuntimeException exception, StringBuilder builder) {
            if (exception.InnerException is ScriptRuntimeException innerexception)
                Traverse(innerexception, builder);
            if (exception.Token is ICodePositionToken codeposition)
                builder.AppendLine($"Line: {codeposition.LineNumber}, Index: {codeposition.TextIndex}");
            builder.AppendLine(exception.Message);
        }

        /// <summary>
        /// creates a stack trace based on script code for a <see cref="ScriptRuntimeException"/>
        /// </summary>
        /// <param name="exception">exception to traverse</param>
        /// <returns>stack trace</returns>
        public static string CreateStackTrace(this ScriptRuntimeException exception) {
            StringBuilder builder=new StringBuilder();
            Traverse(exception, builder);
            return builder.ToString();
        }
    }
}
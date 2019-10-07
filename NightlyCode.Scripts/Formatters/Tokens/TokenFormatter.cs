using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <inheritdoc />
    public abstract class TokenFormatter : ITokenFormatter {

        protected ITokenFormatter GetFormatter(IScriptToken token, Dictionary<Type, ITokenFormatter> formatters) {
            if (!formatters.TryGetValue(token.GetType(), out ITokenFormatter formatter))
                return DefaultFormatter.Instance;
            return formatter;
        }

        protected void FormatBlock(StatementBlock block, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            resulttext.Append(" {\n");
            foreach (IScriptToken token in block.Children) {
                formatters[token].FormatToken(token, resulttext, formatters, depth, true);
            }
            resulttext.Append("}");
        }

        protected void FormatLinkedComments(IScriptToken token, StringBuilder resulttext, int depth = 0) {
            ICommentContainer comments = token as ICommentContainer;
            if (comments?.Comments.Any() ?? false) {
                foreach (string commentline in comments.Comments.Unwrap()) {
                    resulttext.Append("//");
                    if (!commentline.StartsWith(" "))
                        resulttext.Append(" ");
                    resulttext.Append(commentline);
                    resulttext.AppendLine();
                    AppendIntendation(resulttext, depth);
                }
            }
        }

        protected abstract void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0);

        protected void AppendIntendation(StringBuilder resulttext, int depth) {
            for (int i = 0; i < depth; ++i)
                resulttext.Append('\t');
        }

        /// <inheritdoc />
        public void FormatToken(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0, bool mustident=false) {
            if(mustident)
                AppendIntendation(resulttext, depth);
            Format(token, resulttext, formatters, depth);
        }
    }
}
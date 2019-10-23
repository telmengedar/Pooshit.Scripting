
namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token containing comment for a script section
    /// </summary>
    public class Comment : ICodePositionToken {

        /// <summary>
        /// creates a new <see cref="Comment"/>
        /// </summary>
        /// <param name="text">text to include</param>
        /// <param name="textindex">text index where comment is stored</param>
        /// <param name="isMultiline">determines whether comment is multiline</param>
        /// <param name="linenumber">line number where comment is stored</param>
        public Comment(string text, int linenumber, int textindex, bool isMultiline=true) {
            IsMultiline = isMultiline;
            Text = text;
            LineNumber = linenumber;
            TextIndex = textindex;
        }

        /// <summary>
        /// determines whether comment is multiline
        /// </summary>
        public bool IsMultiline { get; }

        /// <summary>
        /// determines whether comment is appended to previous token (only valid for single line comments)
        /// </summary>
        public bool IsPostComment { get; internal set; }

        /// <inheritdoc />
        public string Literal => "//";

        /// <summary>
        /// comment text
        /// </summary>
        public string Text { get; }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            return null;
        }

        /// <inheritdoc />
        public override string ToString() {
            if (IsMultiline)
                return $"/*{Text}*/";
            return $"//{Text}";
        }

        /// <inheritdoc />
        public int LineNumber { get; set; }

        /// <inheritdoc />
        public int TextIndex { get; set; }
    }
}
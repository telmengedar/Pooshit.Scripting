
namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token containing comment for a script section
    /// </summary>
    public class Comment : IScriptToken {

        /// <summary>
        /// creates a new <see cref="Comment"/>
        /// </summary>
        /// <param name="text">text to include</param>
        /// <param name="isMultiline">determines whether comment is multiline</param>
        public Comment(string text, bool isMultiline=true) {
            IsMultiline = isMultiline;
            Text = text;
        }

        /// <summary>
        /// determines whether comment is multiline
        /// </summary>
        public bool IsMultiline { get; }

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
    }
}
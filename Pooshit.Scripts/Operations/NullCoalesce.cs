using Pooshit.Scripting.Data;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations {

    /// <summary>
    /// returns the left-hand side value when it is non-null, otherwise evaluates and returns the right-hand side
    /// </summary>
    public class NullCoalesce : ScriptToken, IBinaryToken, IOperator {

        internal NullCoalesce() {
        }

        /// <inheritdoc />
        public IScriptToken Lhs { get; set; }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public Operator Operator => Operator.NullCoalesce;

        /// <inheritdoc />
        public override string Literal => "??";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return Lhs.Execute(context) ?? Rhs.Execute(context);
        }
    }
}

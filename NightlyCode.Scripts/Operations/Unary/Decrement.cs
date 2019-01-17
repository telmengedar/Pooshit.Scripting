using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// increments the value of a token by 1
    /// </summary>
    public class Decrement : UnaryOperator {
        IAssignableToken token;

        /// <summary>
        /// creates a new <see cref="Decrement"/>
        /// </summary>
        /// <param name="post">determines whether this is a post-increment or a pre-increment</param>
        internal Decrement(bool post) {
            IsPostToken = post;
        }

        /// <inheritdoc />
        protected override object ExecuteToken()
        {
            if (IsPostToken) {
                object result = Operand.Execute();
                token.Assign(new ScriptValue((dynamic) result - 1));
                return result;
            }
            else {
                object result = (dynamic) Operand.Execute() - 1;
                token.Assign(new ScriptValue(result));
                return result;
            }
        }

        /// <inheritdoc />
        public override bool IsPostToken { get; }

        /// <inheritdoc />
        public override IScriptToken Operand {
            get => token;
            set {
                token = value as IAssignableToken;
                if (token == null)
                    throw new ScriptParserException("Operand of decrement must be assignable");
            }
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Decrement;

        /// <inheritdoc />
        public override string ToString() {
            if (IsPostToken)
                return $"{Operand}--";
            return $"--{Operand}";
        }
    }
}
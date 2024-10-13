using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations.Unary {

    /// <summary>
    /// increments the value of a token by 1
    /// </summary>
    public class Increment : UnaryOperator {
        IAssignableToken token;

        /// <summary>
        /// creates a new <see cref="Increment"/>
        /// </summary>
        /// <param name="post">determines whether this is a post-increment or a pre-increment</param>
        internal Increment(bool post) {
            IsPostToken = post;
        }

        /// <inheritdoc />
        public override string Literal => "++";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            if (IsPostToken) {
                object result = Operand.Execute(context);
                token.Assign(new ScriptValue((dynamic) result + 1), context);
                return result;
            }
            else {
                object result = (dynamic) Operand.Execute(context) + 1;
                token.Assign(new ScriptValue(result), context);
                return result;
            }
        }

        /// <inheritdoc />
        public override bool IsPostToken { get; }

        /// <inheritdoc />
        public override IScriptToken Operand {
            get => token;
            set {
                token=value as IAssignableToken;
                if(token==null)
                    // TODO: try to provide position of token
                    throw new ScriptParserException(-1, -1, -1,"Operand of increment must be assignable");
            }
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Increment;

        /// <inheritdoc />
        public override string ToString() {
            if (IsPostToken)
                return $"{Operand}++";
            return $"++{Operand}";
        }
    }
}
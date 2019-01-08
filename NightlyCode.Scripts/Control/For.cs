using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop with an initializer, a condition and an increment
    /// </summary>
    class For : IControlToken {
        readonly IScriptToken initializer;
        readonly IScriptToken condition;
        readonly IScriptToken step;

        /// <summary>
        /// creates a new <see cref="For"/> statement 
        /// </summary>
        /// <param name="loopparameters"></param>
        public For(IScriptToken[] loopparameters) {
            if (loopparameters.Length != 3)
                throw new ScriptParserException("3 loop parameters needed for a 'for' loop");
            initializer = loopparameters[0];
            condition = loopparameters[1];
            step = loopparameters[2];
        }

        /// <inheritdoc />
        public object Execute() {
            initializer?.Execute();

            while (condition.Execute().ToBoolean()) {
                object value=Body?.Execute();
                if (value is Return)
                    return value;
                if (value is Break breaktoken) {
                    --breaktoken.Depth;
                    if(breaktoken.Depth<=0)
                        return null;
                    return breaktoken;
                }

                step?.Execute();
            }

            return null;
        }

        /// <inheritdoc />
        public IScriptToken Body { get; set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"for({initializer},{condition},{step}) {Body}";
        }
    }
}
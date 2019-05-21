using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop with an initializer, a condition and an increment
    /// </summary>
    public class For : ControlToken {
        readonly IScriptToken initializer;
        readonly IScriptToken condition;
        readonly IScriptToken step;

        /// <summary>
        /// creates a new <see cref="For"/> statement 
        /// </summary>
        /// <param name="loopparameters"></param>
        internal For(IScriptToken[] loopparameters) {
            if (loopparameters.Length != 3)
                throw new ScriptParserException("3 loop parameters needed for a 'for' loop");
            initializer = loopparameters[0];
            condition = loopparameters[1];
            step = loopparameters[2];
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            VariableContext loopvariables = new VariableContext(variables);
            initializer?.Execute(loopvariables, arguments);

            while (condition.Execute(loopvariables, arguments).ToBoolean()) {
                object value=Body?.Execute(loopvariables, arguments);
                if (value is Return)
                    return value;
                if (value is Break breaktoken) {
                    int depth = breaktoken.Depth.Execute<int>(loopvariables, arguments);
                    if(depth<=1)
                        return null;
                    return new Break(new ScriptValue(depth - 1));
                }

                if (value is Continue continuetoken) {
                    int depth = continuetoken.Depth.Execute<int>(loopvariables, arguments);
                    if (depth <= 1) {
                        step?.Execute(loopvariables, arguments);
                        continue;
                    }

                    return new Continue(new ScriptValue(depth - 1));
                }

                step?.Execute(loopvariables, arguments);
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"for({initializer}, {condition}, {step}) {Body}";
        }
    }
}
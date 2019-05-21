using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// executes a statement block while a condition is met
    /// </summary>
    public class While : ControlToken {
        readonly IScriptToken condition;

        /// <summary>
        /// creates a new <see cref="While"/>
        /// </summary>
        /// <param name="parameters">parameters containing condition to check</param>
        internal While(IScriptToken[] parameters) {
            if (parameters.Length != 1)
                throw new ScriptParserException("While needs exactly one condition as parameter");
            condition = parameters[0];
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            VariableContext loopvariables = new VariableContext(variables);
            while (condition.Execute(loopvariables, arguments).ToBoolean()) {
                object value=Body.Execute(loopvariables, arguments);
                if (value is Return)
                    return value;
                if (value is Break breaktoken)
                {
                    int depth = breaktoken.Depth.Execute<int>(loopvariables, arguments);
                    if (depth <= 1)
                        return null;
                    return new Break(new ScriptValue(depth - 1));
                }
                if (value is Continue continuetoken)
                {
                    int depth = continuetoken.Depth.Execute<int>(loopvariables, arguments);
                    if (depth <= 1)
                        continue;

                    return new Continue(new ScriptValue(depth - 1));
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }
    }
}
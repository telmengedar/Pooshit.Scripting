using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// executes a statement block while a condition is met
    /// </summary>
    class While : ControlToken {
        readonly IScriptToken condition;

        /// <summary>
        /// creates a new <see cref="While"/>
        /// </summary>
        /// <param name="parameters">parameters containing condition to check</param>
        public While(IScriptToken[] parameters) {
            if (parameters.Length != 1)
                throw new ScriptParserException("While needs exactly one condition as parameter");
            condition = parameters[0];
        }

        /// <inheritdoc />
        protected override object ExecuteToken()
        {
            while (condition.Execute().ToBoolean()) {
                object value=Body.Execute();
                if (value is Return)
                    return value;
                if (value is Break breaktoken)
                {
                    if (breaktoken.Depth <= 1)
                        return null;
                    return new Break(new ScriptValue(breaktoken.Depth - 1));
                }
                if (value is Continue continuetoken)
                {
                    if (continuetoken.Depth <= 1)
                        continue;
                    return new Continue(new ScriptValue(continuetoken.Depth - 1));
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }
    }
}
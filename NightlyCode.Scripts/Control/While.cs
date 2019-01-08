﻿using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// executes a statement block while a condition is met
    /// </summary>
    class While : IControlToken {
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
        public object Execute() {
            while (condition.Execute().ToBoolean()) {
                object value=Body.Execute();
                if (value is Return)
                    return value;
                if (value is Break breaktoken)
                {
                    --breaktoken.Depth;
                    if (breaktoken.Depth <= 0)
                        return null;
                    return breaktoken;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public IScriptToken Body { get; set; }
    }
}
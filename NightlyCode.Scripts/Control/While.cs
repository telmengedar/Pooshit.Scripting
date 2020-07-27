using System.Collections.Generic;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// executes a statement block while a condition is met
    /// </summary>
    public class While : ControlToken, IParameterContainer {
        readonly IScriptToken condition;

        /// <summary>
        /// creates a new <see cref="While"/>
        /// </summary>
        /// <param name="condition">condition to check</param>
        internal While(IScriptToken condition) {
            this.condition = condition;
        }

        /// <inheritdoc />
        public override string Literal => "while";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            ScriptContext loopcontext = new ScriptContext(context);
            while(condition.Execute(loopcontext).ToBoolean()) {
                loopcontext.CancellationToken.ThrowIfCancellationRequested();

                object value = Body.Execute(loopcontext);
                if(value is Return)
                    return value;
                if(value is Break breaktoken) {
                    int depth = breaktoken.Depth.Execute<int>(loopcontext);
                    if(depth <= 1)
                        return null;
                    return new Break(new ScriptValue(depth - 1));
                }
                if(value is Continue continuetoken) {
                    int depth = continuetoken.Depth.Execute<int>(loopcontext);
                    if(depth <= 1)
                        continue;

                    return new Continue(new ScriptValue(depth - 1));
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get { yield return condition; }
        }

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}
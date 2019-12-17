using System.Collections.Generic;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop with an initializer, a condition and an increment
    /// </summary>
    public class For : ControlToken, IParameterContainer {
        readonly IScriptToken initializer;
        readonly IScriptToken condition;
        readonly IScriptToken step;

        /// <summary>
        /// creates a new <see cref="For"/> statement 
        /// </summary>
        /// <param name="initializer">variable initializer</param>
        /// <param name="condition">loop condition</param>
        /// <param name="step">loop step token</param>
        internal For(IScriptToken initializer, IScriptToken condition, IScriptToken step) {
            this.initializer = initializer;
            this.condition = condition;
            this.step = step;
        }

        /// <inheritdoc />
        public override string Literal => "for";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            VariableContext loopvariables = new VariableContext(context.Variables);
            ScriptContext forcontext = new ScriptContext(loopvariables, context.Arguments, context.CancellationToken);
            initializer?.Execute(forcontext);

            while (condition.Execute(forcontext).ToBoolean()) {
                forcontext.CancellationToken.ThrowIfCancellationRequested();

                object value=Body?.Execute(forcontext);
                if (value is Return)
                    return value;
                if (value is Break breaktoken) {
                    int depth = breaktoken.Depth.Execute<int>(forcontext);
                    if(depth<=1)
                        return null;
                    return new Break(new ScriptValue(depth - 1));
                }

                if (value is Continue continuetoken) {
                    int depth = continuetoken.Depth.Execute<int>(forcontext);
                    if (depth <= 1) {
                        step?.Execute(forcontext);
                        continue;
                    }

                    return new Continue(new ScriptValue(depth - 1));
                }

                step?.Execute(forcontext);
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"for({initializer}, {condition}, {step}) {Body}";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get {
                if (initializer != null)
                    yield return initializer;
                if (condition != null)
                    yield return condition;
                if (step != null)
                    yield return step;
            }
        }

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}
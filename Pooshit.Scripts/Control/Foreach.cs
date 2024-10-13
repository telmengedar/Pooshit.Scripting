using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop which iterates over a collection
    /// </summary>
    public class Foreach : ControlToken, IParameterContainer {
        readonly ScriptVariable variable;
        readonly IScriptToken collection;

        /// <summary>
        /// creates a new <see cref="Foreach"/>
        /// </summary>
        /// <param name="variable">iterator variable</param>
        /// <param name="collection">collection which is enumerated</param>
        internal Foreach(ScriptVariable variable, IScriptToken collection) {
            this.variable = variable;
            this.collection = collection;
        }

        /// <summary>
        /// iterator variable
        /// </summary>
        public ScriptVariable Iterator => variable;

        /// <summary>
        /// collection to be iterated
        /// </summary>
        public IScriptToken Collection => collection;

        /// <inheritdoc />
        public override string Literal => "foreach";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            ScriptContext loopcontext = new ScriptContext(context);

            object collectionvalue = collection.Execute(loopcontext);
            if(collectionvalue is IEnumerable enumeration) {
                foreach(object value in enumeration.Cast<object>()) {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    variable.Assign(new ScriptValue(value), loopcontext);
                    object bodyvalue = Body?.Execute(loopcontext);
                    if(bodyvalue is Return)
                        return bodyvalue;
                    if(bodyvalue is Break breaktoken) {
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
            }
            else
                throw new ScriptRuntimeException("Foreach value is not a collection", collection);

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"foreach({variable}, {collection}) {Body}";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get {
                yield return variable;
                yield return collection;
            }
        }

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}
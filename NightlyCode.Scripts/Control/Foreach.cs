using System.Collections;
using System.Linq;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop which iterates over a collection
    /// </summary>
    public class Foreach : ControlToken {
        readonly ScriptVariable variable;
        readonly IScriptToken collection;

        /// <summary>
        /// creates a new <see cref="Foreach"/>
        /// </summary>
        /// <param name="parameters">parameters containing iterator variable and collection to iterate over</param>
        internal Foreach(IScriptToken[] parameters) {
            if (parameters.Length != 2)
                throw new ScriptParserException("Foreach needs a variable and a collection as parameters");

            variable = parameters[0] as ScriptVariable;
            if(variable==null)
                throw new ScriptParserException("Foreach iterator variable has to be a variable token");
            
            collection = parameters[1];
        }

        /// <summary>
        /// iterator variable
        /// </summary>
        public ScriptVariable Iterator => variable;

        /// <summary>
        /// collection to be iterated
        /// </summary>
        public IScriptToken Collection { get; set; }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            VariableContext loopvariables = new VariableContext(variables);
            object collectionvalue = collection.Execute(loopvariables, arguments);
            if (collectionvalue is IEnumerable enumeration) {
                foreach (object value in enumeration.Cast<object>()) {
                    variable.Assign(new ScriptValue(value), loopvariables, arguments);
                    object bodyvalue=Body?.Execute(loopvariables, arguments);
                    if (bodyvalue is Return)
                        return bodyvalue;
                    if (bodyvalue is Break breaktoken)
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
            }
            else throw new ScriptRuntimeException("Foreach value is not a collection");

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"foreach({variable}, {collection}) {Body}";
        }
    }
}
using System.Collections;
using System.Linq;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop which iterates over a collection
    /// </summary>
    public class Foreach : ControlToken {
        readonly IAssignableToken variable;
        readonly IScriptToken collection;

        /// <summary>
        /// creates a new <see cref="Foreach"/>
        /// </summary>
        /// <param name="parameters">parameters containing iterator variable and collection to iterate over</param>
        internal Foreach(IScriptToken[] parameters) {
            if (parameters.Length != 2)
                throw new ScriptParserException("Foreach needs a variable and a collection as parameters");

            variable = parameters[0] as IAssignableToken;
            if(variable==null)
                throw new ScriptParserException("Foreach loop variable has to be a token to which a value can get assigned");
            
            collection = parameters[1];
        }

        /// <inheritdoc />
        protected override object ExecuteToken() {
            object collectionvalue = collection.Execute();
            if (collectionvalue is IEnumerable enumeration) {
                foreach (object value in enumeration.Cast<object>()) {
                    variable.Assign(new ScriptValue(value));
                    object bodyvalue=Body?.Execute();
                    if (bodyvalue is Return)
                        return bodyvalue;
                    if (bodyvalue is Break breaktoken)
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
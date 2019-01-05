using System.Collections;
using System.Linq;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop which iterates over a collection
    /// </summary>
    class Foreach : IControlToken {
        readonly IAssignableToken variable;
        readonly IScriptToken collection;

        /// <summary>
        /// creates a new <see cref="Foreach"/>
        /// </summary>
        /// <param name="parameters">parameters containing iterator variable and collection to iterate over</param>
        public Foreach(IScriptToken[] parameters) {
            if (parameters.Length != 2)
                throw new ScriptParserException("Foreach needs a variable and a collection as parameters");

            variable = parameters[0] as IAssignableToken;
            if(variable==null)
                throw new ScriptParserException("Foreach loop variable has to be a token to which a value can get assigned");
            
            collection = parameters[1];
        }

        /// <inheritdoc />
        public object Execute() {
            object collectionvalue = collection.Execute();
            if (collectionvalue is IEnumerable enumeration) {
                foreach (object value in enumeration.Cast<object>()) {
                    variable.Assign(new ScriptValue(value));
                    object bodyvalue=Body?.Execute();
                    if (bodyvalue is Return)
                        return bodyvalue;
                    if (bodyvalue is Break)
                        return null;
                }
            }
            else throw new ScriptRuntimeException("Foreach value is not a collection");

            return null;
        }

        /// <inheritdoc />
        public IScriptToken Body { get; set; }
    }
}
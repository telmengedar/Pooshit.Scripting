using System.Collections;
using System.Linq;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {
    public class Foreach : IControlToken {
        readonly IAssignableToken variable;
        readonly IScriptToken collection;

        public Foreach(IScriptToken[] parameters) {
            if (parameters.Length != 2)
                throw new ScriptException("Foreach needs a variable and a collection as parameters");

            variable = parameters[0] as IAssignableToken;
            if(variable==null)
                throw new ScriptException("Foreach loop variable has to be a token to which a value can get assigned");
            
            collection = parameters[1];
        }

        public object Execute() {
            object collectionvalue = collection.Execute();
            if (collectionvalue is IEnumerable enumeration) {
                foreach (object value in enumeration.Cast<object>()) {
                    variable.Assign(new ScriptValue(value));
                    object bodyvalue=Body?.Execute();
                    if (bodyvalue is Return)
                        return bodyvalue;
                }
            }
            else throw new ScriptException("Foreach value is not a collection");

            return null;
        }

        public IScriptToken Body { get; set; }
    }
}
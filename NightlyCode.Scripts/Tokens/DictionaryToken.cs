using System.Collections.Generic;
using NightlyCode.Scripting.Extern;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token resulting in a dictionary
    /// </summary>
    public class DictionaryToken : IScriptToken {
        readonly Dictionary<IScriptToken, IScriptToken> dictionary = new Dictionary<IScriptToken, IScriptToken>();

        /// <summary>
        /// adds an entry to the dictionary
        /// </summary>
        /// <param name="key">token resulting in dictionary key</param>
        /// <param name="value">token resulting in dictionary value</param>
        public void Add(IScriptToken key, IScriptToken value) {
            dictionary[key] = value;
        }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            Dictionary<object, object> result=new Dictionary<object, object>();
            foreach (KeyValuePair<IScriptToken, IScriptToken> kvp in dictionary)
                result[kvp.Key.Execute(context)] = kvp.Value?.Execute(context);
            return result;
        }

        /// <inheritdoc />
        public T Execute<T>(ScriptContext context) {
            return Converter.Convert<T>(Execute(context));
        }
    }
}
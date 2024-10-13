using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <inheritdoc />
    public class FormatterCollection : IFormatterCollection {
        readonly Dictionary<Type, ITokenFormatter> formatters = new Dictionary<Type, ITokenFormatter>();

        
        void PrepareHandler(Type key) {
            HashSet<Type> typestosearch = new HashSet<Type>();
            Type current = key.BaseType;
            while (current != null && current != typeof(object)) {
                typestosearch.Add(current);
                current = current.BaseType;
            }

            foreach(Type type in key.GetInterfaces().Reverse())
                typestosearch.Add(type);

            foreach (Type type in typestosearch)
                if (formatters.TryGetValue(type, out ITokenFormatter result)) {
                    formatters[key] = result;

                    // handler was found. break here.
                    return;
                }

            throw new InvalidOperationException($"No handler exists for '{key}'");
        }

        /// <inheritdoc />
        public ITokenFormatter this[IScriptToken token] {
            get {
                if (token == null)
                    throw new ArgumentNullException(nameof(token));
                Type tokentype = token.GetType();

                if (formatters.TryGetValue(tokentype, out ITokenFormatter result))
                    return result;

                PrepareHandler(tokentype);
                return formatters[tokentype];
            }
        }

        /// <summary>
        /// adds a formatter for a specific token type
        /// </summary>
        /// <param name="tokentype">token type for which to set formatter</param>
        /// <param name="formatter">formatter to use for token</param>
        public void AddFormatter(Type tokentype, ITokenFormatter formatter) {
            formatters[tokentype] = formatter;
        }
    }
}
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Providers;

namespace NightlyCode.Scripting.Parser {

    /// <inheritdoc />
    class TypeProvider : ITypeProvider {
        readonly Dictionary<string, ITypeInstanceProvider> types=new Dictionary<string, ITypeInstanceProvider>();

        /// <inheritdoc />
        public ITypeInstanceProvider GetType(string name) {
            if (!types.TryGetValue(name, out ITypeInstanceProvider provider))
                throw new ScriptParserException($"Unknown type '{name}'");
            return provider;
        }

        /// <inheritdoc />
        public void AddType(string name, ITypeInstanceProvider provider) {
            types[name] = provider;
        }

        /// <inheritdoc />
        public void AddType<T>(string name=null) {
            if (name == null)
                name = typeof(T).Name.ToLower();
            AddType(name, new TypeInstanceProvider(typeof(T)));
        }

        /// <inheritdoc />
        public void RemoveType(string name) {
            types.Remove(name);
        }
    }
}
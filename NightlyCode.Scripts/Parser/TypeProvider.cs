using System.Collections.Generic;
using NightlyCode.Scripting.Providers;

namespace NightlyCode.Scripting.Parser {

    /// <inheritdoc />
    class TypeProvider : ITypeProvider {
        readonly Dictionary<string, ITypeInstanceProvider> types=new Dictionary<string, ITypeInstanceProvider>();

        /// <inheritdoc />
        public ITypeInstanceProvider GetType(string name) {
            types.TryGetValue(name, out ITypeInstanceProvider provider);
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

        /// <inheritdoc />
        public bool HasType(string name) {
            return types.ContainsKey(name);
        }

        /// <inheritdoc />
        public IEnumerable<string> ProvidedTypes => types.Keys;
    }
}
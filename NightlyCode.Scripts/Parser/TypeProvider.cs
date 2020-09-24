using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Parser.Resolvers;
using NightlyCode.Scripting.Providers;

namespace NightlyCode.Scripting.Parser {

    /// <inheritdoc />
    public class TypeProvider : ITypeProvider {
        readonly IMethodResolver resolver;
        readonly Dictionary<string, ITypeInstanceProvider> types=new Dictionary<string, ITypeInstanceProvider>();
        
        /// <summary>
        /// creates a new <see cref="TypeProvider"/>
        /// </summary>
        /// <param name="resolver">provided to typeinstance provider</param>
        public TypeProvider(IMethodResolver resolver) {
            this.resolver = resolver ?? new MethodResolver(null);
        }

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
        public void AddType(Type type, string name = null) {
            if(name == null)
                name = type.Name.ToLower();
            AddType(name, new TypeInstanceProvider(type, resolver));
        }

        /// <inheritdoc />
        public void AddType<T>(string name=null) {
            AddType(typeof(T), name);
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
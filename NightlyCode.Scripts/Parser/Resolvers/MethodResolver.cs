using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser.Resolvers {

    /// <summary>
    /// default method provider
    /// </summary>
    public class MethodResolver : IMethodResolver {
        readonly IExtensionProvider extensions;
        readonly ConcurrentDictionary<MethodCacheKey, IResolvedMethod> methodcache = new ConcurrentDictionary<MethodCacheKey, IResolvedMethod>();
        readonly ConcurrentDictionary<MethodCacheKey, ConstructorInfo> constructorcache = new ConcurrentDictionary<MethodCacheKey, ConstructorInfo>();

        /// <summary>
        /// creates a new <see cref="MethodResolver"/>
        /// </summary>
        /// <param name="extensions">access to extension methods</param>
        public MethodResolver(IExtensionProvider extensions) {
            this.extensions = extensions;
        }

        /// <summary>
        /// determines whether to cache resolved methods for later calls
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <inheritdoc />
        public IResolvedMethod Resolve(object host, string methodname, object[] parameters, ReferenceParameter[] referenceparameters) {
            MethodCacheKey cachekey=null;
            if (EnableCaching) {
                cachekey = new MethodCacheKey(host?.GetType(), methodname, parameters.Select(p => p?.GetType()).ToArray(), referenceparameters);
                if (methodcache.TryGetValue(cachekey, out IResolvedMethod method))
                    return method;
            }

            MethodInfo[] methods = host.GetType().GetMethods().Where(m => m.Name.ToLower() == methodname && MethodOperations.MatchesParameterCount(m, parameters.Length)).ToArray();

            Tuple<MethodInfo, int>[] evaluation = methods.Select(m => MethodOperations.GetMethodMatchValue(m, parameters)).Where(e => e.Item2 >= 0).OrderBy(m => m.Item2).ToArray();
            if (evaluation.Length > 0) {
                if (EnableCaching)
                    return methodcache[cachekey] = new ResolvedMethod(evaluation[0].Item1, referenceparameters);
                return new ResolvedMethod(evaluation[0].Item1, referenceparameters);
            }

            if (extensions != null)
            {
                Type extensionbase = host.GetType();
                while (extensionbase != null)
                {
                    Type lookuptype = extensionbase;
                    if (lookuptype.IsGenericType)
                        lookuptype = lookuptype.GetGenericTypeDefinition();

                    methods = extensions.GetExtensions(lookuptype).Where(m => m.Name.ToLower() == methodname && MethodOperations.MatchesParameterCount(m, parameters.Length, true)).ToArray();
                    evaluation = methods.Select(m => MethodOperations.GetMethodMatchValue(m, parameters, true)).OrderBy(m => m.Item2).ToArray();
                    if (evaluation.Length > 0)
                    {
                        MethodInfo method = evaluation[0].Item1;
                        if (method.IsGenericMethodDefinition)
                            method = method.MakeGenericMethod(extensionbase.GetGenericArguments());

                        if (EnableCaching)
                            return methodcache[cachekey] = new ResolvedMethod(method, referenceparameters, true);
                        return new ResolvedMethod(method, referenceparameters, true);
                    }

                    if (extensionbase == typeof(object))
                        break;
                    extensionbase = extensionbase.BaseType;
                }


                foreach (Type interfacetype in host.GetType().GetInterfaces().OrderBy(i => i.IsGenericType ? 0 : 1))
                {
                    Type lookuptype = interfacetype;
                    if (lookuptype.IsGenericType)
                        lookuptype = lookuptype.GetGenericTypeDefinition();

                    methods = extensions.GetExtensions(lookuptype).Where(m => m.Name.ToLower() == methodname && MethodOperations.MatchesParameterCount(m, parameters.Length, true)).ToArray();
                    evaluation = methods.Select(m => MethodOperations.GetMethodMatchValue(m, parameters, true)).OrderBy(m => m.Item2).ToArray();
                    if (evaluation.Length > 0)
                    {
                        MethodInfo method = evaluation[0].Item1;
                        if (method.IsGenericMethodDefinition)
                            method = method.MakeGenericMethod(interfacetype.GetGenericArguments());

                        if (EnableCaching)
                            return methodcache[cachekey] = new ResolvedMethod(method, referenceparameters, true);
                        return new ResolvedMethod(method, referenceparameters, true);
                    }
                }
            }

            throw new ScriptRuntimeException($"Method '{methodname}' matching the parameters '({string.Join(",", parameters)})' not found on type {host.GetType().Name}", null);
        }

        /// <inheritdoc />
        public ConstructorInfo ResolveConstructor(Type type, object[] parameters) {
            MethodCacheKey methodkey=null;
            if (EnableCaching) {
                methodkey = new MethodCacheKey(type, parameters.Select(p => p?.GetType()).ToArray());
                if (constructorcache.TryGetValue(methodkey, out ConstructorInfo method))
                    return method;
            }

            ConstructorInfo[] constructors = type.GetConstructors().Where(c => MethodOperations.MatchesParameterCount(c, parameters.Length)).ToArray();

            if (constructors.Length == 0)
                throw new ScriptRuntimeException($"No matching constructors available for '{type.Name}({string.Join(",", parameters)})'", null);

            Tuple<ConstructorInfo, int>[] evaluated = constructors.Select(c => MethodOperations.GetMethodMatchValue(c, parameters)).Where(e => e.Item2 >= 0).ToArray();
            if (evaluated.Length == 0)
                throw new ScriptRuntimeException($"No matching constructor found for '{type.Name}({string.Join(", ", parameters)})'", null);

            if(EnableCaching)
                return constructorcache[methodkey]= evaluated[0].Item1;
            return evaluated[0].Item1;
        }
    }
}
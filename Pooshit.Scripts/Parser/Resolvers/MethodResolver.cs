using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Operations;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Parser.Resolvers {

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

        MethodInfo[] GetCandidates(IEnumerable<MethodInfo> methods, string method, object[] parameters, Type[] genericParameters, bool isExtension) {
            if (genericParameters == null)
                return methods.Where(m => m.Name.ToLower() == method && !m.IsGenericMethodDefinition && MethodOperations.MatchesParameterCount(m, parameters.Length, isExtension)).ToArray();
            
            return methods
                .Where(m => m.Name.ToLower() == method && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == genericParameters.Length && MethodOperations.MatchesParameterCount(m, parameters.Length, isExtension))
                .Select(m => m.MakeGenericMethod(genericParameters))
                .ToArray();
        }
        
        /// <inheritdoc />
        public IResolvedMethod Resolve(object host, string methodname, object[] parameters, ReferenceParameter[] referenceparameters, Type[] genericparameters=null) {
            MethodCacheKey cachekey=null;
            if (EnableCaching) {
                cachekey = new MethodCacheKey(host?.GetType(), methodname, parameters.Select(p => p?.GetType()).ToArray(), referenceparameters, genericparameters);
                if (methodcache.TryGetValue(cachekey, out IResolvedMethod method))
                    return method;
            }

            MethodInfo[] methods = GetCandidates(host.GetType().GetMethods(), methodname, parameters, genericparameters, false);
            
            List<Tuple<Type, MethodInfo, int, bool>> evaluation = new List<Tuple<Type, MethodInfo, int, bool>>(methods.Select(m => {
                int result = MethodOperations.GetMethodMatchValue(m, parameters);
                return new Tuple<Type, MethodInfo, int, bool>(host.GetType(), m, result, false);
            }).Where(m => m.Item3 >= 0));
            
            if (extensions != null)
            {
                Type extensionbase = host.GetType();
                while (extensionbase != null)
                {
                    Type lookuptype = extensionbase;
                    if (lookuptype.IsGenericType)
                        lookuptype = lookuptype.GetGenericTypeDefinition();

                    methods = GetCandidates(extensions.GetExtensions(lookuptype), methodname, parameters, genericparameters, true);
                    evaluation.AddRange(methods.Select(m => {
                        int result = MethodOperations.GetMethodMatchValue(m, parameters, true);
                        return new Tuple<Type, MethodInfo, int, bool>(extensionbase, m, result, true);
                    }).Where(e => e.Item3 >= 0));

                    if (extensionbase == typeof(object))
                        break;
                    extensionbase = extensionbase.BaseType;
                }


                foreach (Type interfacetype in host.GetType().GetInterfaces().OrderBy(i => i.IsGenericType ? 0 : 1))
                {
                    Type lookuptype = interfacetype;
                    if (lookuptype.IsGenericType)
                        lookuptype = lookuptype.GetGenericTypeDefinition();

                    methods = GetCandidates(extensions.GetExtensions(lookuptype), methodname, parameters, genericparameters, true);
                    evaluation.AddRange(methods.Select(m => {
                        int result = MethodOperations.GetMethodMatchValue(m, parameters, true);
                        return new Tuple<Type, MethodInfo, int, bool>(extensionbase, m, result, true);
                    }).Where(e => e.Item3 >= 0));
                }
            }

            if (evaluation.Count > 0) {
                Tuple<Type, MethodInfo, int, bool> methodInformation = evaluation.OrderBy(e=>e.Item3).First();
                MethodInfo method = methodInformation.Item2;

                if (EnableCaching)
                    return methodcache[cachekey] = new ResolvedMethod(method, referenceparameters, methodInformation.Item4);
                return new ResolvedMethod(method, referenceparameters, methodInformation.Item4);
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

            Tuple<ConstructorInfo, int>[] evaluated = constructors.Select(c => new Tuple<ConstructorInfo, int>(c, MethodOperations.GetMethodMatchValue(c, parameters))).Where(e => e.Item2 >= 0).OrderBy(e=>e.Item2).ToArray();
            if (evaluated.Length == 0)
                throw new ScriptRuntimeException($"No matching constructor found for '{type.Name}({string.Join(", ", parameters)})'", null);

            if(EnableCaching)
                return constructorcache[methodkey]= evaluated[0].Item1;
            return evaluated[0].Item1;
        }
    }
}
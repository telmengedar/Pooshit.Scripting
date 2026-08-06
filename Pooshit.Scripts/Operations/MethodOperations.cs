using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations {

    /// <summary>
    /// operations used when calling methods dynamically
    /// </summary>
    static class MethodOperations {
        static readonly Type[] floatlist = {
            typeof(float), typeof(double), typeof(decimal)
        };

        static readonly Type[] integerlist = {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)
        };

        public static bool IsFloatingPoint(Type type) {
            return Array.IndexOf(floatlist, type) > -1;
        }

        public static bool IsInteger(Type type) {
            return Array.IndexOf(integerlist, type) > -1;
        }

        /// <summary>
        /// scores how well a candidate method's parameters match a set of call-site argument values
        /// </summary>
        /// <remarks>
        /// A <see cref="ScriptContext"/> parameter is supplied by the engine, not by the script call: it
        /// consumes no script argument and contributes nothing to the match score, so it is skipped via
        /// <c>continue</c> without advancing <c>index</c> (the source-argument cursor, tracked separately
        /// from the target-parameter cursor <c>i</c> for exactly this reason).
        /// </remarks>
        /// <param name="method">method to score</param>
        /// <param name="parameters">call-site argument values</param>
        /// <param name="isextension">determines whether the method is an extension method (its first parameter is skipped)</param>
        /// <returns>match score, or -1 when the method cannot be called with these parameters</returns>
        public static int GetMethodMatchValue<T>(T method, object[] parameters, bool isextension = false)
        where T : MethodBase {
            int result = 0; //isextension ? 0 : 0;
            ParameterInfo[] methodparameters = method.GetParameters();
            if (isextension) methodparameters = methodparameters.Skip(1).ToArray();
            bool hasparams = methodparameters.Length > 0 && Attribute.IsDefined(methodparameters.Last(), typeof(ParamArrayAttribute));

            int index=0;
            for(int i=0;i<methodparameters.Length;++i) {
                if (methodparameters[i].ParameterType == typeof(ScriptContext))
                    continue;

                int multiplicator = 1;
                Type methodparameter = i == methodparameters.Length - 1 && (hasparams || methodparameters[i].ParameterType.IsByRef) ? methodparameters[i].ParameterType.GetElementType() : methodparameters[i].ParameterType;

                if (index >= parameters.Length)
                    return result;

                object parameter = parameters[index++];
                if (i == methodparameters.Length - 1 && hasparams) {
                    if (parameter is Array array) {
                        if (array.Length == 0)
                            continue;
                        parameter = array.GetValue(0);
                    }
                    else if (parameter is IEnumerable enumerable && !(parameter is string) && !(parameter is Dictionary<object, object>)) {
                        if (!enumerable.Cast<object>().Any())
                            continue;
                        parameter = enumerable.Cast<object>().First();
                    }

                    multiplicator = 100;
                }

                if (parameter == null) {
                    if (methodparameter.IsValueType && !(methodparameter.IsGenericType && methodparameter.GetGenericTypeDefinition()==typeof(Nullable<>)))
                        return -1;
                    continue;
                }

                if (methodparameter.IsArray) {
                    
                    if (parameter is Array array) {
                        if (array.Length == 0)
                            continue;
                        parameter = array.GetValue(0);
                    }
                    else if (parameter is IEnumerable enumerable && !(parameter is string) && !(parameter is Dictionary<object, object>)) {
                        if (!enumerable.Cast<object>().Any())
                            continue;
                        parameter = enumerable.Cast<object>().First();
                    }

                    methodparameter = methodparameter.GetElementType();
                    multiplicator = 100;
                }
                else if (typeof(IEnumerable).IsAssignableFrom(methodparameter) && methodparameter != typeof(string)) {
                    if (parameter is Array array)
                    {
                        if (array.Length == 0)
                            continue;
                        parameter = array.GetValue(0);
                    }
                    else if (parameter is IEnumerable enumerable && !(parameter is string))
                    {
                        if (!enumerable.Cast<object>().Any())
                            continue;
                        parameter = enumerable.Cast<object>().First();
                    }

                    methodparameter = methodparameter.IsGenericType ? methodparameter.GetGenericArguments()[0] : typeof(object);
                    multiplicator = 100;
                }

                if (methodparameter == typeof(object)) {
                    result += 100*multiplicator;
                    continue;
                }

                if (!(parameter is string) && !(parameter is Dictionary<object, object>) && (parameter is Array || parameter is IEnumerable))
                    return -1;

                if (methodparameter == parameter.GetType() || (parameter.GetType().IsNullable() && parameter.GetType().GetGenericArguments()[0] == methodparameter))
                    continue;

                if (methodparameter == typeof(string)) {
                    result += 80;
                    continue;
                }

                if (IsFloatingPoint(parameter.GetType())) {
                    if (!IsFloatingPoint(methodparameter))
                        return -1;
                    result += 15 * multiplicator;
                    continue;
                }

                if (IsInteger(parameter.GetType()))
                {
                    if (IsInteger(methodparameter))
                        result+= 3 * multiplicator;
                    else if (IsFloatingPoint(methodparameter))
                        result += 8 * multiplicator;
                    else if (methodparameter == typeof(char))
                        result += 12 * multiplicator;
                    else if (methodparameter.IsEnum)
                        result += 4 * multiplicator;
                    else return -1;
                    continue;
                }

                if (parameter is char) {
                    if (IsInteger(methodparameter))
                        result += 5 * multiplicator;
                    else return -1;
                    continue;
                }

                if (methodparameter != null && methodparameter.IsInstanceOfType(parameter)) {
                    result += 30 * multiplicator;
                    continue;
                }

                if (parameter is string) {
                    if (methodparameter.IsEnum)
                        result += 20 * multiplicator;
                    else result += 50 * multiplicator;
                    continue;
                }

                if (parameter is Dictionary<object, object>) {
                    result += 120 * multiplicator;
                    continue;
                }

                if (parameter is ScriptType && typeof(Type).IsAssignableFrom(methodparameter)) {
                    result += 30 * multiplicator;
                    continue;
                }

                return -1;
            }

            return result;
        }

        /// <summary>
        /// determines whether a method could be called using the provided parameters
        /// </summary>
        /// <remarks>
        /// This does not determine whether the parameter types actually match, it only determines whether
        /// the parameter count matches. A <see cref="ScriptContext"/> parameter is supplied by the engine,
        /// not the script call, so it is excluded from both the required and the maximum parameter counts.
        /// </remarks>
        /// <param name="method">method to check</param>
        /// <param name="parametercount">number of parameters</param>
        /// <param name="isextension">determines whether the method is an extension method</param>
        /// <returns>true if method count matches, false otherwise</returns>
        public static bool MatchesParameterCount(MethodBase method, int parametercount, bool isextension = false) {
            ParameterInfo[] methodparameters = method.GetParameters().Where(p => p.ParameterType != typeof(ScriptContext)).ToArray();
            bool hasparams = methodparameters.Length > 0 && Attribute.IsDefined(methodparameters.Last(), typeof(ParamArrayAttribute));
            int minimumcount = methodparameters.Count(p => !p.HasDefaultValue);
            if (hasparams)
                --minimumcount;

            if (isextension)
                // extension methods have their first parameter specified by script engine
                ++parametercount;

            if (parametercount < minimumcount)
                return false;

            if (hasparams)
                return true;

            return parametercount <= methodparameters.Length;
        }

        /// <summary>
        /// calls a constructor using the specified parameters
        /// </summary>
        /// <param name="constructor">constructor to call</param>
        /// <param name="parameters">parameters for constructor</param>
        /// <param name="context">execution context</param>
        /// <returns>constructed object</returns>
        public static object CallConstructor(ConstructorInfo constructor, IScriptToken[] parameters, ScriptContext context) {
            ParameterInfo[] targetparameters = constructor.GetParameters();
            object[] callparameters = parameters.Select(p => p.Execute(context)).ToArray();

            try {
                callparameters = CreateParameters(targetparameters, callparameters).ToArray();
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert parameters for {constructor}", null, e);
            }

            try {
                return constructor.Invoke(callparameters);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to call {constructor}", null, e);
            }
        }

        /// <summary>
        /// converts script argument values to reflection call parameters and invokes a resolved host method
        /// </summary>
        /// <remarks>
        /// An engine-driven cancellation or step-limit abort raised by a reflected host call (eg. a
        /// <see cref="ScriptContext"/>-guarded <c>EnumerableExtensions</c>/<c>TaskHost</c> method, or a
        /// lambda invoked from within one) must reach the caller unwrapped, exactly like any other
        /// checkpoint. Reflection always wraps it in a <see cref="TargetInvocationException"/>, so it is
        /// unwrapped explicitly rather than falling into the generic <see cref="ScriptRuntimeException"/>
        /// translation used for every other failure. An ordinary <see cref="ScriptRuntimeException"/> raised
        /// by the host method itself is deliberately still wrapped, unchanged, so the calling site remains
        /// part of the diagnostic. The same abort or cancellation may also arrive wrapped a layer deeper, in
        /// a flattened <see cref="AggregateException"/> (eg. from <c>Task.WaitAll</c>) inside the
        /// <see cref="TargetInvocationException"/>; <see cref="IsAbortOrCancellation"/> and
        /// <see cref="UnwrapAbortOrCancellation"/> cover both the direct and the wrapped route.
        /// </remarks>
        /// <param name="methodcall">token that triggered the call, used for error reporting</param>
        /// <param name="host">host instance the method is called on (null for extension/static methods)</param>
        /// <param name="method">resolved method to call</param>
        /// <param name="parameters">script-evaluated argument values</param>
        /// <param name="context">execution context, injected into any <see cref="ScriptContext"/> parameter</param>
        /// <param name="refparameters">reference/out parameter bindings to write back after the call</param>
        /// <param name="extension">determines whether the method is an extension method</param>
        /// <returns>result of the method call</returns>
        public static object CallMethod(IScriptToken methodcall, object host, MethodInfo method, object[] parameters, ScriptContext context, IEnumerable<ReferenceParameter> refparameters=null, bool extension=false) {
            ParameterInfo[] targetparameters = method.GetParameters();

            object[] callparameters;
            try {
                if (extension)
                    callparameters = CreateParameters(host, targetparameters.Skip(1).ToArray(), parameters, context).ToArray();
                else callparameters = CreateParameters(targetparameters, parameters, context).ToArray();
            }
            catch (ScriptRuntimeException e) {
                throw new ScriptRuntimeException($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))})\n{e.Message}", methodcall, e);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))})\n{string.Join("\r\n", parameters.Select(p => p.ToString()))}", methodcall, e);
            }

            try {
                object result= method.Invoke(extension ? null : host, callparameters);
                if (refparameters != null) {
                    foreach (ReferenceParameter param in refparameters)
                        param.Variable.Assign(new ScriptValue(callparameters[param.Index+(extension?1:0)]), context);
                }

                return result;
            }
            catch (TargetInvocationException e) when (IsAbortOrCancellation(e.InnerException)) {
                throw UnwrapAbortOrCancellation(e.InnerException);
            }
            catch (TargetInvocationException e) {
                throw new ScriptRuntimeException($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)})\n{e.InnerException?.Message ?? e.Message}", methodcall, e.InnerException ?? e);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)})\n{e.Message}", methodcall, e);
            }
        }

        /// <summary>
        /// determines whether <paramref name="exception"/> is, or contains via a flattened
        /// <see cref="AggregateException"/>, a cancellation or an engine abort
        /// </summary>
        /// <param name="exception">exception to inspect</param>
        /// <returns>true if <paramref name="exception"/> is or carries a passthrough-worthy exception</returns>
        static bool IsAbortOrCancellation(Exception exception) {
            return exception switch {
                OperationCanceledException or ScriptAbortException => true,
                AggregateException aggregate => aggregate.Flatten().InnerExceptions.Any(IsAbortOrCancellation),
                _ => false
            };
        }

        /// <summary>
        /// extracts the cancellation or engine abort exception found by <see cref="IsAbortOrCancellation"/>
        /// </summary>
        /// <param name="exception">exception <see cref="IsAbortOrCancellation"/> returned true for</param>
        /// <returns>the exception to rethrow</returns>
        static Exception UnwrapAbortOrCancellation(Exception exception) {
            return exception is AggregateException aggregate
                ? aggregate.Flatten().InnerExceptions.First(IsAbortOrCancellation)
                : exception;
        }

        /// <summary>
        /// converts script argument values to reflection call parameters, injecting <paramref name="context"/>
        /// into any target parameter of type <see cref="ScriptContext"/>
        /// </summary>
        /// <param name="targetparameters">parameters of the method being called</param>
        /// <param name="sourceparameters">script-evaluated argument values</param>
        /// <param name="context">execution context to inject; null when the caller has none (eg. constructors)</param>
        /// <returns>parameters ready to pass to <see cref="MethodBase.Invoke(object,object[])"/></returns>
        public static IEnumerable<object> CreateParameters(ParameterInfo[] targetparameters, object[] sourceparameters, ScriptContext context = null) {
            return CreateParameters(null, targetparameters, sourceparameters, context);
        }

        public static object ConvertParameter(object value, Type targettype) {
            if (targettype.IsArray)
            {
                Type elementtype = targettype.GetElementType();
                if (value is Array valuearray)
                {
                    Array array = Array.CreateInstance(elementtype, valuearray.Length);
                    for (int k = 0; k < array.Length; ++k) {
                        object item = valuearray.GetValue(k);
                        if (item is IDictionary dictionary)
                            array.SetValue(dictionary.ToType(elementtype), k);
                        else array.SetValue(Converter.Convert(item, elementtype), k);
                    }

                    return array;
                }
                else
                {
                    Array array = Array.CreateInstance(elementtype, 1);
                    if (value is IDictionary dictionary)
                        array.SetValue(dictionary.ToType(elementtype), 0);
                    else array.SetValue(Converter.Convert(value, elementtype), 0);
                    return array;
                }
            }

            if (targettype.IsGenericType) {
                if (targettype.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                    Type genericargument = targettype.GetGenericArguments()[0];
                    if (value is Array valuearray)
                    {
                        Array array = Array.CreateInstance(genericargument, valuearray.Length);
                        for (int k = 0; k < array.Length; ++k) {
                            object item = valuearray.GetValue(k);
                            if(item is IDictionary dictionary)
                                array.SetValue(Converter.Convert(dictionary.ToType(genericargument), genericargument), k);
                            else array.SetValue(Converter.Convert(item, genericargument), k);
                        }

                        return array;
                    }
                    else
                    {
                        Array array = Array.CreateInstance(genericargument, 1);
                        if (value is IDictionary dictionary)
                            array.SetValue(dictionary.ToType(genericargument), 0);
                        else array.SetValue(value, 0);
                        return array;
                    }
                }
            }

            if (targettype == typeof(IEnumerable))
            {
                if (!(value is IEnumerable))
                    return Converter.Convert(value, targettype);
            }
            else if (value is IDictionary dictionary) {
                return dictionary.ToType(targettype);
            }
            else if (targettype == typeof(string))
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            else {
                MethodInfo implicitcast = targettype.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => {
                        if (m.Name != "op_Implicit" || m.ReturnType != targettype)
                            return false;

                        ParameterInfo[] parameters = m.GetParameters();
                        if (parameters.Length != 1)
                            return false;

                        return parameters[0].ParameterType == value.GetType();
                    }).FirstOrDefault();

                if (implicitcast != null)
                    return implicitcast.Invoke(null, new[] {value});
                    
                return Converter.Convert(value, targettype);
            }

            return null;
        }

        /// <summary>
        /// converts script argument values to reflection call parameters, injecting <paramref name="context"/>
        /// into any target parameter of type <see cref="ScriptContext"/>
        /// </summary>
        /// <remarks>
        /// <c>sourceindex</c> tracks the position within <paramref name="sourceparameters"/> separately from
        /// the target-parameter index <c>i</c>, since an injected <see cref="ScriptContext"/> parameter
        /// consumes a target slot but no script argument.
        /// </remarks>
        /// <param name="staticparameter">extended-type instance to yield first for an extension method call, or null</param>
        /// <param name="targetparameters">parameters of the method being called (excluding the extended type for an extension method)</param>
        /// <param name="sourceparameters">script-evaluated argument values</param>
        /// <param name="context">execution context to inject; null when the caller has none (eg. constructors)</param>
        /// <returns>parameters ready to pass to <see cref="MethodBase.Invoke(object,object[])"/></returns>
        public static IEnumerable<object> CreateParameters(object staticparameter, ParameterInfo[] targetparameters, object[] sourceparameters, ScriptContext context = null)
        {
            if (staticparameter != null)
                yield return staticparameter;

            int sourceindex = 0;
            for (int i = 0; i < targetparameters.Length; ++i) {
                ParameterInfo targetparameter = targetparameters[i];

                if (targetparameter.ParameterType == typeof(ScriptContext)) {
                    yield return context;
                    continue;
                }

                if (sourceindex >= sourceparameters.Length) {
                    if (Attribute.IsDefined(targetparameter, typeof(ParamArrayAttribute))) {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        yield return Array.CreateInstance(targetparameter.ParameterType.GetElementType(), 0);
                        continue;
                    }
                    if (!targetparameter.HasDefaultValue)
                        throw new ScriptRuntimeException($"Unable to create parameter {i}. No value specified and parameter doesn't provide a default value.", null);
                    yield return targetparameter.DefaultValue;
                    continue;
                }

                object value;
                if (i == targetparameters.Length - 1 && Attribute.IsDefined(targetparameter, typeof(ParamArrayAttribute))) {
                    Type targettype = targetparameter.ParameterType.GetElementType();
                    if (targettype == null)
                        throw new ScriptRuntimeException("Methodparameter without type detected", null);

                    Array sourcearray = null;
                    if (sourceindex == sourceparameters.Length - 1) {
                        value = sourceparameters[sourceindex];
                        if (value is Array array)
                            sourcearray = array;
                        else if (value is IEnumerable enumerable && !(value is string))
                            sourcearray = enumerable.Cast<object>().ToArray();
                    }

                    if (sourcearray == null)
                        sourcearray = sourceparameters.Skip(sourceindex).ToArray();

                    Array targetarray = Array.CreateInstance(targettype, sourcearray.Length);
                    for (int k = 0; k < targetarray.Length; ++k)
                        targetarray.SetValue(ConvertParameter(sourcearray.GetValue(k), targettype), k);
                    yield return targetarray;
                    yield break;
                }

                value = sourceparameters[sourceindex];
                ++sourceindex;
                if (value == null) {
                    if (targetparameter.ParameterType.IsValueType && !targetparameter.ParameterType.IsNullable())
                        throw new ScriptRuntimeException($"Unable to convert null to {targetparameter.ParameterType.Name} since a valuetype is needed", null);
                    yield return null;
                    continue;
                }

                if (targetparameter.ParameterType.IsInstanceOfType(value)) {
                    yield return value;
                    continue;
                }

                try {
                    value = ConvertParameter(value, targetparameter.ParameterType.IsByRef ? targetparameter.ParameterType.GetElementType() : targetparameter.ParameterType);
                }
                catch (Exception e) {
                    throw new ScriptRuntimeException($"Unable to convert parameter {i} ({value}) to {targetparameter.ParameterType}", null, e);
                }

                yield return value;
            }

        }

    }
}
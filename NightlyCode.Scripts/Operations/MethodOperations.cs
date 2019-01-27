using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations {

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

        public static Tuple<T,int> GetMethodMatchValue<T>(T method, object[] parameters, bool isextension = false)
        where T : MethodBase
        {
            int result = isextension ? 1000 : 0;
            ParameterInfo[] methodparameters = method.GetParameters();
            if (isextension) methodparameters = methodparameters.Skip(1).ToArray();
            bool hasparams = methodparameters.Length > 0 && Attribute.IsDefined(methodparameters.Last(), typeof(ParamArrayAttribute));

            int index=0;
            for(int i=0;i<methodparameters.Length;++i) {
                Type methodparameter = i == methodparameters.Length - 1 && hasparams ? methodparameters[i].ParameterType.GetElementType() : methodparameters[i].ParameterType;

                if (index >= parameters.Length)
                    return new Tuple<T, int>(method, result);

                object parameter = parameters[index++];
                if (i == methodparameters.Length - 1 && hasparams) {
                    if (parameter is Array array) {
                        if (array.Length == 0)
                            continue;
                        parameter = array.GetValue(0);
                    }
                    else if (parameter is IEnumerable enumerable) {
                        if (!enumerable.Cast<object>().Any())
                            continue;
                        parameter = enumerable.Cast<object>().First();
                    }
                }

                if (parameter == null) {
                    if (methodparameter.IsValueType)
                        return new Tuple<T, int>(method, -1);
                    continue;
                }

                if (methodparameter.IsArray) {
                    
                    if (parameter is Array array) {
                        if (array.Length == 0)
                            continue;
                        parameter = array.GetValue(0);
                    }
                    else if (parameter is IEnumerable enumerable) {
                        if (!enumerable.Cast<object>().Any())
                            continue;
                        parameter = enumerable.Cast<object>().First();
                    }

                    methodparameter = methodparameter.GetElementType();
                }
                else if (typeof(IEnumerable).IsAssignableFrom(methodparameter)) {
                    if (parameter is Array array)
                    {
                        if (array.Length == 0)
                            continue;
                        parameter = array.GetValue(0);
                    }
                    else if (parameter is IEnumerable enumerable)
                    {
                        if (!enumerable.Cast<object>().Any())
                            continue;
                        parameter = enumerable.Cast<object>().First();
                    }

                    methodparameter = methodparameter.IsGenericType ? methodparameter.GetGenericArguments()[0] : typeof(object);
                }

                if (methodparameter == typeof(object)) {
                    result += 80;
                    continue;
                }

                if (methodparameter == parameter.GetType())
                    continue;

                if (methodparameter == typeof(string)) {
                    result += 200;
                    continue;
                }

                if (IsFloatingPoint(parameter.GetType())) {
                    if (!IsFloatingPoint(methodparameter))
                        return new Tuple<T, int>(method, -1);
                    result += 15;
                    continue;
                }

                if (IsInteger(parameter.GetType()))
                {
                    if (IsInteger(methodparameter))
                        result+= 3;
                    else if (IsFloatingPoint(methodparameter))
                        result += 8;
                    else if (methodparameter == typeof(char))
                        result += 12;
                    else if (methodparameter.IsEnum)
                        result += 4;
                    else return new Tuple<T, int>(method, -1);
                    continue;
                }

                if (parameter is char) {
                    if (IsInteger(methodparameter))
                        result += 5;
                    else return new Tuple<T, int>(method, -1);
                    continue;
                }

                if (methodparameter != null && methodparameter.IsInstanceOfType(parameter)) {
                    result += 40;
                    continue;
                }

                if (parameter is string) {
                    if (methodparameter.IsEnum)
                        result += 20;
                    else result += 120;
                    continue;
                }

                return new Tuple<T, int>(method, -1);
            }

            return new Tuple<T, int>(method, result);
        }

        /// <summary>
        /// determines whether a method could be called using the provided parameters 
        /// </summary>
        /// <remarks>
        /// this does not determine whether the parameter types actually matches, it only determines whether the parameter count matches
        /// </remarks>
        /// <param name="method">method to check</param>
        /// <param name="parameters">specified script parameters</param>
        /// <param name="isextension">determines whether the method is an extension method</param>
        /// <returns>true if method count matches, false otherwise</returns>
        public static bool MatchesParameterCount(MethodBase method, IScriptToken[] parameters, bool isextension = false) {
            ParameterInfo[] methodparameters = method.GetParameters();
            bool hasparams = methodparameters.Length > 0 && Attribute.IsDefined(methodparameters.Last(), typeof(ParamArrayAttribute));
            int minimumcount = methodparameters.Count(p => !p.HasDefaultValue);
            if (hasparams)
                --minimumcount;
            if (isextension)
                // extension methods have their first parameter specified by script engine
                --minimumcount;

            if (parameters.Length < minimumcount)
                return false;

            if (hasparams)
                return true;

            return parameters.Length <= methodparameters.Length;
        }

        /// <summary>
        /// calls a constructor using the specified parameters
        /// </summary>
        /// <param name="constructor">constructor to call</param>
        /// <param name="parameters">parameters for constructor</param>
        /// <returns></returns>
        public static object CallConstructor(ConstructorInfo constructor, IScriptToken[] parameters) {
            ParameterInfo[] targetparameters = constructor.GetParameters();
            object[] callparameters = parameters.Select(p => p.Execute()).ToArray();

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

        public static object CallMethod(object host, MethodInfo method, object[] parameters, bool extension=false, ParameterInfo[] targetparameters=null) {
            if(targetparameters==null)
                targetparameters = method.GetParameters();

            object[] callparameters;
            try {
                if (extension)
                    callparameters = CreateParameters(host, targetparameters.Skip(1).ToArray(), parameters).ToArray();
                else callparameters = CreateParameters(targetparameters, parameters).ToArray();
            }
            catch (ScriptRuntimeException e) {
                throw new ScriptRuntimeException($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))})", e.Message);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))})", string.Join("\r\n", parameters.Select(p => p.ToString())), e);
            }

            try {
                return method.Invoke(extension ? null : host, callparameters);
            }
            catch (TargetInvocationException e) {
                throw new ScriptRuntimeException($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)})", e.InnerException?.Message ?? e.Message, e);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)})", e.Message, e);
            }
        }

        public static IEnumerable<object> CreateParameters(ParameterInfo[] targetparameters, object[] sourceparameters) {
            return CreateParameters(null, targetparameters, sourceparameters);
        }

        public static object ConvertParameter(object value, Type targettype) {
            if (targettype.IsArray)
            {
                Type elementtype = targettype.GetElementType();
                if (value is Array valuearray)
                {
                    Array array = Array.CreateInstance(elementtype, valuearray.Length);
                    for (int k = 0; k < array.Length; ++k)
                        array.SetValue(Converter.Convert(valuearray.GetValue(k), elementtype), k);
                    return array;
                }
                else
                {
                    Array array = Array.CreateInstance(elementtype, 1);
                    array.SetValue(value, 0);
                    return array;
                }
            }

            if (targettype.IsGenericType) {
                if (targettype.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                    Type genericargument = targettype.GetGenericArguments()[0];
                    if (value is Array valuearray)
                    {
                        Array array = Array.CreateInstance(genericargument, valuearray.Length);
                        for (int k = 0; k < array.Length; ++k)
                            array.SetValue(Converter.Convert(valuearray.GetValue(k), genericargument), k);
                        return array;
                    }
                    else
                    {
                        Array array = Array.CreateInstance(genericargument, 1);
                        array.SetValue(value, 0);
                        return array;
                    }
                }
            }

            if (targettype == typeof(IEnumerable))
            {
                if (!(value is IEnumerable))
                    return Converter.Convert(value, targettype);
            }
            else return Converter.Convert(value, targettype);

            return null;
        }

        public static IEnumerable<object> CreateParameters(object staticparameter, ParameterInfo[] targetparameters, object[] sourceparameters)
        {
            if (staticparameter != null)
                yield return staticparameter;

            for (int i = 0; i < targetparameters.Length; ++i) {
                ParameterInfo targetparameter = targetparameters[i];
                if (i >= sourceparameters.Length) {
                    if (Attribute.IsDefined(targetparameter, typeof(ParamArrayAttribute))) {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        yield return Array.CreateInstance(targetparameter.ParameterType.GetElementType(), 0);
                        continue;
                    }
                    if (!targetparameter.HasDefaultValue)
                        throw new ScriptRuntimeException($"Unable to create parameter {i}. No value specified and parameter doesn't provide a default value.");
                    yield return targetparameter.DefaultValue;
                    continue;
                }

                object value;
                if (i == targetparameters.Length - 1 && Attribute.IsDefined(targetparameter, typeof(ParamArrayAttribute))) {
                    Type targettype = targetparameter.ParameterType.GetElementType();
                    if (targettype == null)
                        throw new ScriptRuntimeException("Methodparameter without type detected");

                    if (i >= sourceparameters.Length) {
                        yield return Array.CreateInstance(targettype, 0);
                        yield break;
                    }

                    Array sourcearray = null;
                    if (i == sourceparameters.Length - 1) {
                        value = sourceparameters[i];
                        if (value is Array array)
                            sourcearray = array;
                        else if (value is IEnumerable enumerable && !(value is string))
                            sourcearray = enumerable.Cast<object>().ToArray();
                    }

                    if (sourcearray == null)
                        sourcearray = sourceparameters.Skip(i).ToArray();

                    Array targetarray = Array.CreateInstance(targettype, sourcearray.Length);
                    for (int k = 0; k < targetarray.Length; ++k)
                        targetarray.SetValue(ConvertParameter(sourcearray.GetValue(k), targettype), k);
                    yield return targetarray;
                    yield break;
                }

                value = sourceparameters[i];
                if (value == null) {
                    if (targetparameter.ParameterType.IsValueType)
                        throw new ScriptRuntimeException($"Unable to convert null to {targetparameter.ParameterType.Name} since a valuetype is needed");
                    yield return null;
                    continue;
                }

                if (targetparameter.ParameterType.IsInstanceOfType(value)) {
                    yield return value;
                    continue;
                }

                try {
                    value = ConvertParameter(value, targetparameter.ParameterType);
                }
                catch (Exception e) {
                    throw new ScriptRuntimeException($"Unable to convert parameter {i} ({value}) to {targetparameter.ParameterType}", null, e);
                }

                yield return value;
            }

        }

    }
}
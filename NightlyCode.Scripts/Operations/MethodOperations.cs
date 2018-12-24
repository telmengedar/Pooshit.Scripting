using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NightlyCode.Core.Conversion;

namespace NightlyCode.Scripting.Operations {
    public static class MethodOperations {

        public static object CallMethod(object host, MethodInfo method, IScriptToken[] parameters, bool extension=false, ParameterInfo[] targetparameters=null, IScriptToken additionalparameters=null) {
            if(targetparameters==null)
                targetparameters = method.GetParameters();
            object[] callparameters;
            try {
                if (extension)
                    callparameters = CreateParameters(host, targetparameters.Skip(1).ToArray(), parameters).ToArray();
                else callparameters = CreateParameters(targetparameters, parameters).ToArray();
            }
            catch (ScriptException e) {
                throw new ScriptException($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))})", e.Message);
            }
            catch (Exception e) {
                throw new ScriptException($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))})", string.Join("\r\n", parameters.Select(p => p.ToString())), e);
            }

            try {
                if (additionalparameters != null)
                    return method.Invoke(extension ? null : host, callparameters.Concat(new[] {additionalparameters.Execute()}).ToArray());
                return method.Invoke(extension ? null : host, callparameters);
            }
            catch (TargetInvocationException e) {
                throw new ScriptException($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)})", e.InnerException?.Message ?? e.Message, e);
            }
            catch (Exception e) {
                throw new ScriptException($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)})", e.Message, e);
            }
        }

        public static IEnumerable<object> CreateParameters(ParameterInfo[] targetparameters, IScriptToken[] sourceparameters) {
            return CreateParameters(null, targetparameters, sourceparameters);
        }

        public static IEnumerable<object> CreateParameters(object staticparameter, ParameterInfo[] targetparameters, IScriptToken[] sourceparameters)
        {
            if (staticparameter != null)
                yield return staticparameter;

            for (int i = 0; i < targetparameters.Length; ++i)
            {
                object value = sourceparameters[i].Execute();
                if (value == null) {
                    if (targetparameters[i].ParameterType.IsValueType)
                        throw new ScriptException($"Unable to convert null to {targetparameters[i].ParameterType.Name} since a valuetype is needed");
                    yield return null;
                    continue;
                }

                if (targetparameters[i].ParameterType.IsInstanceOfType(value)) {
                    yield return value;
                    continue;
                }

                try {
                    if (targetparameters[i].ParameterType.IsArray) {
                        Type elementtype = targetparameters[i].ParameterType.GetElementType();
                        if (value is Array valuearray) {
                            Array array = Array.CreateInstance(elementtype, valuearray.Length);
                            for (int k = 0; k < array.Length; ++k)
                                array.SetValue(Converter.Convert(valuearray.GetValue(k), elementtype), k);
                            value = array;
                        }
                        else {
                            Array array = Array.CreateInstance(elementtype, 1);
                            array.SetValue(value, 0);
                            value = array;
                        }
                    }
                    else if (targetparameters[i].ParameterType == typeof(IEnumerable)) {
                        if (!(value is IEnumerable))
                            value = Converter.Convert(value, targetparameters[i].ParameterType);
                    }
                    else value = Converter.Convert(value, targetparameters[i].ParameterType);
                }
                catch (Exception e) {
                    throw new ScriptException($"Unable to convert parameter {i} ({value}) to {targetparameters[i].ParameterType}", null, e);
                }

                yield return value;
            }

        }

    }
}
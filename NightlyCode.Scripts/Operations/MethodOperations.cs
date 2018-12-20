using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NightlyCode.Core.Conversion;

namespace NightlyCode.Scripting.Operations {
    public static class MethodOperations {

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
                if (targetparameters[i].ParameterType.IsArray)
                {
                    if (value == null)
                        yield return null;
                    else
                    {
                        Type elementtype = targetparameters[i].ParameterType.GetElementType();
                        if (value is Array valuearray)
                        {
                            Array array = Array.CreateInstance(elementtype, valuearray.Length);
                            for (int k = 0; k < array.Length; ++k)
                                array.SetValue(Converter.Convert(valuearray.GetValue(k), elementtype), k);
                            yield return array;
                        }
                        else
                        {
                            Array array = Array.CreateInstance(elementtype, 1);
                            array.SetValue(value, 0);
                            yield return array;
                        }
                    }
                }
                else if (targetparameters[i].ParameterType == typeof(IEnumerable))
                {
                    if (value is IEnumerable)
                        yield return value;
                    else yield return Converter.Convert(value, targetparameters[i].ParameterType);
                }
                else yield return Converter.Convert(value, targetparameters[i].ParameterType);
            }

        }

    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using NightlyCode.Scripting.Extern;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extension methods used for dictionary types
    /// </summary>
    public static class DictionaryExtensions {

        /// <summary>
        /// converts values of a dictionary to a type
        /// </summary>
        /// <param name="dictionary">dictionary containing property values</param>
        /// <param name="targettype">type to create</param>
        /// <returns>type created from dictionary</returns>
        public static object ToType(this Dictionary<object, object> dictionary, Type targettype) {
            object value = Activator.CreateInstance(targettype, true);
            return FillType(dictionary, value);
        }


        public static object FillType(this Dictionary<object, object> dictionary, object value) {
            Type targettype = value.GetType();
            foreach(KeyValuePair<object, object> property in dictionary) {
                string propertyname = property.Key.ToString();
                PropertyInfo propertyinfo = targettype.GetProperty(propertyname, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                if(propertyinfo == null)
                    continue;

                if(property.Value is Dictionary<object, object> subdictionary && propertyinfo.PropertyType != typeof(Dictionary<object, object>)) {
                    propertyinfo.SetValue(value, ToType(subdictionary, propertyinfo.PropertyType));
                }
                else if(propertyinfo.PropertyType.IsArray) {
                    Array arrayvalue;
                    Type elementtype = propertyinfo.PropertyType.GetElementType();
                    if(property.Value is Array sourcearray) {
                        arrayvalue = Array.CreateInstance(elementtype, sourcearray.Length);
                        for(int i = 0; i < sourcearray.Length; ++i) {
                            if(sourcearray.GetValue(i) is Dictionary<object, object> itemdictionary)
                                arrayvalue.SetValue(itemdictionary.ToType(elementtype), i);
                            else
                                arrayvalue.SetValue(Converter.Convert(sourcearray.GetValue(i), elementtype), i);
                        }
                    }
                    else {
                        arrayvalue = Array.CreateInstance(propertyinfo.PropertyType.GetElementType(), 1);
                        if(property.Value is Dictionary<object, object> itemdictionary)
                            arrayvalue.SetValue(itemdictionary.ToType(elementtype), 0);
                        else
                            arrayvalue.SetValue(Converter.Convert(property.Value, elementtype), 0);
                    }

                    propertyinfo.SetValue(value, arrayvalue);
                }
                else
                    propertyinfo.SetValue(value, Converter.Convert(property.Value, propertyinfo.PropertyType, true));
            }

            return value;
        }

    }
}
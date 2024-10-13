﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pooshit.Scripting.Extern;

namespace Pooshit.Scripting.Extensions {

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
        public static object ToType(this IDictionary dictionary, Type targettype) {
            object value = Activator.CreateInstance(targettype, true);
            return FillType(dictionary, value);
        }

        /// <summary>
        /// converts values of a dictionary to a type
        /// </summary>
        /// <param name="dictionary">dictionary containing property values</param>
        /// <returns>type created from dictionary</returns>
        public static T ToType<T>(this IDictionary dictionary) {
            return (T)ToType(dictionary, typeof(T));
        }

        /// <summary>
        /// converts values of a dictionary to a type
        /// </summary>
        /// <remarks>
        /// this is necessary because expandoobjects just implement idictionary&lt;string,object&gt;
        /// but not idictionary
        /// </remarks>
        /// <param name="dictionary">dictionary containing property values</param>
        /// <param name="targettype">type to create</param>
        /// <returns>type created from dictionary</returns>
        public static object ToType(this IDictionary<string,object> dictionary, Type targettype) {
            object value = Activator.CreateInstance(targettype, true);
            return FillType(dictionary, value);
        }

        /// <summary>
        /// fills a type with information stored in a dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object FillType(this IDictionary dictionary, object value) {
            Type targettype = value.GetType();
            foreach(DictionaryEntry property in dictionary) {
                string propertyname = property.Key.ToString();
                PropertyInfo propertyinfo = targettype.GetProperty(propertyname, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                if(propertyinfo == null || property.Value==null)
                    continue;

                if (property.Value.GetType() == propertyinfo.PropertyType || propertyinfo.PropertyType.IsInstanceOfType(property.Value)) {
                    propertyinfo.SetValue(value, property.Value);
                    continue;
                }
                
                if(property.Value is IDictionary subdictionary) {
                    propertyinfo.SetValue(value, ToType(subdictionary, propertyinfo.PropertyType));
                }
                else if(propertyinfo.PropertyType.IsArray) {
                    Array arrayvalue;
                    Type elementtype = propertyinfo.PropertyType.GetElementType();
                    if(property.Value is IEnumerable sourcelist) {
                        Array sourcearray = sourcelist as Array ?? sourcelist.Cast<object>().ToArray();
                        arrayvalue = Array.CreateInstance(elementtype, sourcearray.Length);
                        for(int i = 0; i < sourcearray.Length; ++i) {
                            if(sourcearray.GetValue(i) is IDictionary itemdictionary)
                                arrayvalue.SetValue(itemdictionary.ToType(elementtype), i);
                            else
                                arrayvalue.SetValue(Converter.Convert(sourcearray.GetValue(i), elementtype), i);
                        }
                    }
                    else {
                        arrayvalue = Array.CreateInstance(propertyinfo.PropertyType.GetElementType(), 1);
                        if(property.Value is IDictionary itemdictionary)
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
        
        /// <summary>
        /// fills a type with information stored in a dictionary
        /// </summary>
        /// <param name="dictionary">dictionary from which to take data</param>
        /// <param name="value">value to which to transfer data</param>
        /// <returns></returns>
        public static object FillType(this IDictionary<string,object> dictionary, object value) {
            Type targettype = value.GetType();
            foreach(KeyValuePair<string, object> property in dictionary) {
                string propertyname = property.Key;
                PropertyInfo propertyinfo = targettype.GetProperty(propertyname, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                if(propertyinfo == null || property.Value==null)
                    continue;

                if (property.Value.GetType() == propertyinfo.PropertyType || propertyinfo.PropertyType.IsInstanceOfType(property.Value)) {
                    propertyinfo.SetValue(value, property.Value);
                    continue;
                }
                
                if(property.Value is IDictionary subdictionary) {
                    propertyinfo.SetValue(value, ToType(subdictionary, propertyinfo.PropertyType));
                }
                else if(propertyinfo.PropertyType.IsArray) {
                    Array arrayvalue;
                    Type elementtype = propertyinfo.PropertyType.GetElementType();
                    if(property.Value is IEnumerable sourcelist) {
                        Array sourcearray = sourcelist as Array ?? sourcelist.Cast<object>().ToArray();
                        arrayvalue = Array.CreateInstance(elementtype, sourcearray.Length);
                        for(int i = 0; i < sourcearray.Length; ++i) {
                            if(sourcearray.GetValue(i) is IDictionary itemdictionary)
                                arrayvalue.SetValue(itemdictionary.ToType(elementtype), i);
                            else
                                arrayvalue.SetValue(Converter.Convert(sourcearray.GetValue(i), elementtype), i);
                        }
                    }
                    else {
                        arrayvalue = Array.CreateInstance(propertyinfo.PropertyType.GetElementType(), 1);
                        if(property.Value is IDictionary itemdictionary)
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
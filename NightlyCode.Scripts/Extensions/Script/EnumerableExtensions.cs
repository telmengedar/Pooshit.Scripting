using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Providers;

namespace NightlyCode.Scripting.Extensions.Script {

    /// <summary>
    /// extensions for scripts with <see cref="IEnumerable"/> results
    /// </summary>
    public class EnumerableExtensions {

        /// <summary>
        /// filters an enumeration
        /// </summary>
        /// <param name="enumeration">enumeration to filter</param>
        /// <param name="predicate">predicate to use as filter criterias</param>
        /// <returns>filtered enumeration</returns>
        public static IEnumerable Where(IEnumerable enumeration, LambdaMethod predicate) {
            return enumeration.Cast<object>().Where(i => (bool) predicate.Invoke(i));
        }

        /// <summary>
        /// returns first element of enumeration or default of type
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>first element of enumeration or null if enumeration contains no elements</returns>
        public static object FirstOrDefault(IEnumerable enumeration) {
            return enumeration.Cast<object>().FirstOrDefault();
        }

        /// <summary>
        /// returns first element of enumeration or default of type
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>first element of enumeration or null if enumeration contains no elements</returns>
        public static T FirstOrDefault<T>(IEnumerable<T> enumeration) {
            return enumeration.FirstOrDefault();
        }

        /// <summary>
        /// returns first element of enumeration
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>first element of enumeration</returns>
        public static object First(IEnumerable enumeration) {
            return enumeration.Cast<object>().First();
        }

        /// <summary>
        /// returns first element of enumeration
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>first element of enumeration</returns>
        public static object First<T>(IEnumerable<T> enumeration) {
            return enumeration.Last();
        }

        /// <summary>
        /// get last element of enumeration or null if enumeration contains no elements
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>last element of enumeration or null if enumeration contains no elements</returns>
        public static object LastOrDefault(IEnumerable enumeration)
        {
            return enumeration.Cast<object>().LastOrDefault();
        }

        /// <summary>
        /// get last element of enumeration
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>last element of enumeration</returns>
        public static object Last(IEnumerable enumeration)
        {
            return enumeration.Cast<object>().Last();
        }

        /// <summary>
        /// get minimum value of enumeration of values
        /// </summary>
        /// <param name="enumeration">enumeration of values</param>
        /// <returns>minimum value of enumeration</returns>
        public static object Min(IEnumerable enumeration) {
            return enumeration.Cast<object>().Min();
        }

        /// <summary>
        /// get maximum value of enumeration of values
        /// </summary>
        /// <param name="enumeration">enumeration of values</param>
        /// <returns>maximum value of enumeration</returns>
        public static object Max(IEnumerable enumeration)
        {
            return enumeration.Cast<object>().Max();
        }

        /*/// <summary>
        /// get average of value of enumeration of values
        /// </summary>
        /// <param name="enumeration">enumeration of values</param>
        /// <returns>average value of enumeration</returns>
        public static object Average(IEnumerable enumeration) {
            return enumeration.Cast<object>().Select(v=>Converter.Convert<decimal>(v)).Average();
        }

        /// <summary>
        /// get sum of values of enumeration of values
        /// </summary>
        /// <param name="enumeration">enumeration of values</param>
        /// <returns>sum of values</returns>
        public static object Sum(IEnumerable enumeration) {
            return enumeration.Cast<object>().Select(v=>Converter.Convert<decimal>(v)).Sum();
        }*/

        /// <summary>
        /// get an enumeration of ordered elements
        /// </summary>
        /// <param name="enumeration">enumeration of values</param>
        /// <returns>ordered enumeration of values</returns>
        public static IEnumerable Order(IEnumerable enumeration) {
            return enumeration.Cast<object>().OrderBy(e => e);
        }

        /// <summary>
        /// get an enumeration of values in descending order 
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>values in descending order</returns>
        public static IEnumerable OrderDesc(IEnumerable enumeration) {
            return enumeration.Cast<object>().OrderByDescending(e => e);
        }

        /// <summary>
        /// get the number of elements in an enumeration
        /// </summary>
        /// <param name="enumeration">enumeration</param>
        /// <returns>number of elements in enumeration</returns>
        public static int Count(IEnumerable enumeration) {
            return enumeration.Cast<object>().Count();
        }

        /// <summary>
        /// converts an enumeration to an array
        /// </summary>
        /// <param name="enumeration">enumeration to convert</param>
        /// <returns>array containing elements of the enumeration</returns>
        public static Array ToArray(IEnumerable enumeration) {
            return enumeration.Cast<object>().ToArray();
        }

        /// <summary>
        /// returns index of first matching item 
        /// </summary>
        /// <param name="enumeration">enumeration to iterate</param>
        /// <param name="value">item to match against</param>
        /// <returns>index of first item which matches predicate</returns>
        public static int IndexOf(IEnumerable enumeration, object value) {
            int index = 0;
            if (value == null) {
                foreach (object item in enumeration) {
                    if (item==null)
                        return index;
                    ++index;
                }
            }
            else {
                foreach (object item in enumeration) {
                    if (value.Equals(item))
                        return index;
                    ++index;
                }
            }

            return -1;
        }

        /// <summary>
        /// returns index of first item which matches a predicate 
        /// </summary>
        /// <param name="enumeration">enumeration to iterate</param>
        /// <param name="predicate">predicate for item to match</param>
        /// <returns>index of first item which matches predicate</returns>
        public static int IndexOf(IEnumerable enumeration, LambdaMethod predicate) {
            int index = 0;
            foreach (object item in enumeration) {
                if (predicate.Invoke(item) is bool result && result)
                    return index;
                ++index;
            }

            return -1;
        }

        /// <summary>
        /// returns last item which matches a value 
        /// </summary>
        /// <param name="enumeration">enumeration to iterate</param>
        /// <param name="value">item to match</param>
        /// <returns>index of last item which matches predicate</returns>
        public static int LastIndexOf(IEnumerable enumeration, object value) {
            int lastIndexOf = -1;
            int index = 0;

            if (value == null) {
                foreach (object item in enumeration) {
                    if (item == null)
                        lastIndexOf = index;
                    ++index;
                }
            }
            else {
                foreach (object item in enumeration) {
                    if (value.Equals(item))
                        lastIndexOf = index;
                    ++index;
                }
            }

            return lastIndexOf;
        }

        /// <summary>
        /// returns index of last item which matches a predicate 
        /// </summary>
        /// <param name="enumeration">enumeration to iterate</param>
        /// <param name="predicate">predicate for item to match</param>
        /// <returns>index of last item which matches predicate</returns>
        public static int LastIndexOf(IEnumerable enumeration, LambdaMethod predicate) {
            int lastIndexOf = -1;
            int index = 0;
            
            foreach (object item in enumeration) {
                if (predicate.Invoke(item) is bool result && result)
                    lastIndexOf = index;
                ++index;
            }

            return lastIndexOf;
        }
    }
}
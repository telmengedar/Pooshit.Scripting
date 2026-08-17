using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Pooshit.Scripting.Errors;

namespace Pooshit.Scripting;

/// <summary>
/// approximates the retained footprint of a script variable's value without reflection or unbounded traversal,
/// except for the cached, per-type <see cref="Capacity"/>/<see cref="DictionaryCapacity"/> lookups used to
/// charge pre-sized-but-empty collections, and the pre-allocation operation table pre-allocation charge sites
/// match against
/// </summary>
static class VariableSizer {

    const int MaxSizeDepth = 4;
    const long ObjectHeaderBytes = 24;
    const long ReferenceSize = 8;
    internal const long DictionaryEntryOverhead = 24;
    internal const long CollectionElementOverhead = 8;
    internal const long StringCharBytes = 2;
    internal const long SplitSegmentBytes = ObjectHeaderBytes + ReferenceSize;
    const long OpaqueValueBytes = 64;
    const long ValueTypeBytes = 24;

    static readonly string[] DictionaryEntriesFieldNames = {"_entries", "entries"};

    static readonly ConcurrentDictionary<Type, Func<object, long>> capacityAccessors = new();
    static readonly ConcurrentDictionary<Type, Func<object, long>> dictionaryCapacityAccessors = new();

    /// <summary>
    /// approximates the footprint of <paramref name="value"/>
    /// </summary>
    /// <param name="value">value to size</param>
    /// <returns>approximated footprint in bytes</returns>
    public static long Size(object value) {
        long units = 0;
        return Size(value, ref units);
    }

    /// <summary>
    /// approximates the footprint of <paramref name="value"/>, accumulating visited nodes into <paramref name="units"/>
    /// </summary>
    /// <param name="value">value to size</param>
    /// <param name="units">running count of nodes visited across the walk this call is part of</param>
    /// <returns>approximated footprint in bytes</returns>
    public static long Size(object value, ref long units) => SizeCore(value, 0, ref units);

    static long SizeCore(object value, int depth, ref long units) {
        units++;

        if (value == null)
            return 0;
        if (value is ValueType)
            return ValueTypeBytes;
        if (value is string s)
            return ObjectHeaderBytes + StringCharBytes * s.Length;

        switch (value) {
            case byte[] array: return ObjectHeaderBytes + array.Length;
            case sbyte[] array: return ObjectHeaderBytes + array.Length;
            case bool[] array: return ObjectHeaderBytes + array.Length;
            case char[] array: return ObjectHeaderBytes + StringCharBytes * array.Length;
            case short[] array: return ObjectHeaderBytes + 2L * array.Length;
            case ushort[] array: return ObjectHeaderBytes + 2L * array.Length;
            case int[] array: return ObjectHeaderBytes + 4L * array.Length;
            case uint[] array: return ObjectHeaderBytes + 4L * array.Length;
            case long[] array: return ObjectHeaderBytes + 8L * array.Length;
            case ulong[] array: return ObjectHeaderBytes + 8L * array.Length;
            case float[] array: return ObjectHeaderBytes + 4L * array.Length;
            case double[] array: return ObjectHeaderBytes + 8L * array.Length;
            case decimal[] array: return ObjectHeaderBytes + 16L * array.Length;
            case Array array: return SizeArray(array, depth, ref units);
            case IDictionary dictionary: return SizeDictionary(dictionary, depth, ref units);
            case ICollection collection: return SizeCollection(collection, depth, ref units);
            default: return OpaqueValueBytes;
        }
    }

    static long SizeArray(Array array, int depth, ref long units) {
        long size = ObjectHeaderBytes + array.Length * ReferenceSize;
        if (depth >= MaxSizeDepth)
            return size;

        foreach (object element in array)
            size += SizeCore(element, depth + 1, ref units);
        return size;
    }

    static long SizeDictionary(IDictionary dictionary, int depth, ref long units) {
        long size = ObjectHeaderBytes + DictionaryChargedCount(dictionary) * DictionaryEntryOverhead;
        if (depth >= MaxSizeDepth)
            return size;

        foreach (DictionaryEntry entry in dictionary) {
            size += SizeCore(entry.Key, depth + 1, ref units);
            size += SizeCore(entry.Value, depth + 1, ref units);
        }
        return size;
    }

    static long SizeCollection(ICollection collection, int depth, ref long units) {
        long size = ObjectHeaderBytes + ChargedCount(collection) * CollectionElementOverhead;
        if (depth >= MaxSizeDepth)
            return size;

        foreach (object element in collection)
            size += SizeCore(element, depth + 1, ref units);
        return size;
    }

    /// <summary>
    /// the larger of <paramref name="collection"/>'s live element count and its allocated backing capacity, so
    /// a pre-sized-but-empty collection (eg. <c>new List&lt;object&gt;(capacity)</c>) is charged for the
    /// backing array it already holds rather than the zero elements it happens to contain
    /// </summary>
    static long ChargedCount(ICollection collection) {
        Func<object, long> capacity = Capacity(collection.GetType());
        return capacity == null ? collection.Count : Math.Max(collection.Count, capacity(collection));
    }

    /// <summary>
    /// the larger of <paramref name="dictionary"/>'s live entry count and its allocated bucket/entry
    /// footprint (<see cref="DictionaryCapacity"/>), so a pre-sized-but-empty dictionary (eg. after
    /// <c>EnsureCapacity(n)</c>) is charged for the backing arrays it already holds rather than the zero
    /// entries it happens to contain (design §8.8.4)
    /// </summary>
    static long DictionaryChargedCount(IDictionary dictionary) {
        Func<object, long> capacity = DictionaryCapacity(dictionary.GetType());
        return capacity == null ? dictionary.Count : Math.Max(dictionary.Count, capacity(dictionary));
    }

    /// <summary>
    /// cached accessor for a public, readable, integer-valued <c>Capacity</c> property on <paramref name="type"/>,
    /// or <c>null</c> if <paramref name="type"/> exposes none
    /// </summary>
    static Func<object, long> Capacity(Type type) => capacityAccessors.GetOrAdd(type, BuildCapacityAccessor);

    static Func<object, long> BuildCapacityAccessor(Type type) {
        PropertyInfo property = type.GetProperty("Capacity", BindingFlags.Public | BindingFlags.Instance);
        if (property == null || !property.CanRead || property.GetIndexParameters().Length > 0)
            return null;
        if (property.PropertyType != typeof(int) && property.PropertyType != typeof(long))
            return null;

        return instance => Convert.ToInt64(property.GetValue(instance));
    }

    /// <summary>
    /// cached accessor for <paramref name="type"/>'s private backing entries array (<c>Dictionary&lt;,&gt;</c>
    /// exposes no public <c>Capacity</c> getter), or <c>null</c> if none of <see cref="DictionaryEntriesFieldNames"/>
    /// resolves - deliberately tried across CLR field-name shapes so the same accessor keeps working whichever
    /// runtime the netstandard2.0/net8.0 build actually executes on (design §8.8.4, pinned by T47)
    /// </summary>
    static Func<object, long> DictionaryCapacity(Type type) => dictionaryCapacityAccessors.GetOrAdd(type, BuildDictionaryCapacityAccessor);

    static Func<object, long> BuildDictionaryCapacityAccessor(Type type) {
        foreach (string name in DictionaryEntriesFieldNames) {
            FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && typeof(Array).IsAssignableFrom(field.FieldType))
                return instance => (field.GetValue(instance) as Array)?.Length ?? 0;
        }

        return null;
    }

    /// <summary>
    /// current allocated capacity of an already-existing <paramref name="receiver"/>, read through whichever
    /// of <see cref="Capacity"/> (public property) or <see cref="DictionaryCapacity"/> (private field)
    /// applies, or 0 if <paramref name="receiver"/> exposes neither - the M4 pre-allocation charge sites use
    /// this to compute the delta actually about to be allocated (design §8.8.3/§8.8.4)
    /// </summary>
    internal static long CurrentCapacity(object receiver) {
        if (receiver == null)
            return 0;

        Type type = receiver.GetType();
        Func<object, long> accessor = Capacity(type) ?? DictionaryCapacity(type);
        return accessor?.Invoke(receiver) ?? 0;
    }

    /// <summary>
    /// matches a resolved constructor or method against the pre-allocation operation table and, on a match,
    /// projects the bytes it is about to allocate from <paramref name="host"/>, <paramref name="member"/> and
    /// <paramref name="arguments"/> - covering both the original single-<c>int</c> capacity ops on
    /// <see cref="List{T}"/>/<see cref="Dictionary{TKey,TValue}"/> and the size/format/bulk-argument
    /// projections over <see cref="string"/> and <see cref="List{T}"/> bulk mutation
    /// </summary>
    /// <param name="host">live receiver instance, or <c>null</c> for a constructor call</param>
    /// <param name="member">resolved constructor or method</param>
    /// <param name="arguments">call-site argument values, already converted to the member's parameter types</param>
    /// <param name="projectedBytes">bytes about to be allocated if this is a pre-allocation operation</param>
    /// <returns>true if <paramref name="member"/> is a pre-allocation operation on a table-registered type</returns>
    internal static bool TryGetPreAllocationOperation(object host, MethodBase member, object[] arguments, out long projectedBytes) {
        projectedBytes = 0;
        if (member == null)
            return false;

        Type receiverType = host?.GetType() ?? member.DeclaringType;
        if (receiverType == null)
            return false;

        Type key = receiverType.IsGenericType ? receiverType.GetGenericTypeDefinition() : receiverType;
        if (key == typeof(List<>) || key == typeof(Dictionary<,>))
            return TryGetCollectionCapacityProjection(key, member, arguments, host, out projectedBytes)
                   || (key == typeof(List<>) && TryGetListBulkProjection(member, arguments, out projectedBytes));

        if (receiverType == typeof(string))
            return TryGetStringProjection(member, arguments, host as string, out projectedBytes);

        return false;
    }

    /// <summary>
    /// matches a single-<c>int</c> ctor or <c>EnsureCapacity(int)</c> on <see cref="List{T}"/> or
    /// <see cref="Dictionary{TKey,TValue}"/>, projecting the delta between the requested and current capacity
    /// </summary>
    /// <param name="key">generic type definition of the receiver</param>
    /// <param name="member">resolved constructor or method</param>
    /// <param name="arguments">call-site argument values</param>
    /// <param name="host">live receiver instance, or <c>null</c> for a constructor call</param>
    /// <param name="projectedBytes">bytes about to be allocated if this is a capacity operation</param>
    /// <returns>true if <paramref name="member"/> is a capacity operation</returns>
    static bool TryGetCollectionCapacityProjection(Type key, MethodBase member, object[] arguments, object host, out long projectedBytes) {
        projectedBytes = 0;
        if (member.Name != ".ctor" && member.Name != "EnsureCapacity")
            return false;

        ParameterInfo[] parameters = member.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(int))
            return false;

        long bytesperunit = key == typeof(List<>) ? CollectionElementOverhead : DictionaryEntryOverhead;
        long requested = Math.Max(0, Convert.ToInt64(arguments[0]) - CurrentCapacity(host));
        projectedBytes = requested * bytesperunit;
        return true;
    }

    /// <summary>
    /// matches <see cref="List{T}.AddRange"/>/<see cref="List{T}.InsertRange"/>, projecting the bulk
    /// argument's <see cref="ICollection.Count"/>; refuses a bulk argument whose count cannot be read before
    /// enumeration rather than silently skipping the charge
    /// </summary>
    /// <param name="member">resolved method</param>
    /// <param name="arguments">call-site argument values</param>
    /// <param name="projectedBytes">bytes about to be allocated if this is a bulk mutation</param>
    /// <returns>true if <paramref name="member"/> is a governed bulk mutation</returns>
    static bool TryGetListBulkProjection(MethodBase member, object[] arguments, out long projectedBytes) {
        projectedBytes = 0;

        int argumentindex;
        switch (member.Name) {
            case "AddRange": argumentindex = 0; break;
            case "InsertRange": argumentindex = 1; break;
            default: return false;
        }

        if (arguments == null || argumentindex >= arguments.Length)
            return false;

        if (!(arguments[argumentindex] is ICollection collection))
            throw new ScriptRuntimeException($"'{member.Name.ToLower()}' requires an argument whose element count is known before enumeration; materialise it first (e.g. '.toarray()')", null);

        projectedBytes = collection.Count * CollectionElementOverhead;
        return true;
    }

    /// <summary>
    /// matches the size/amplification-bearing members of <see cref="string"/>: <c>PadRight</c>/<c>PadLeft</c>,
    /// <c>.ctor(char,int)</c>, <c>Replace</c>, <c>ReplaceLineEndings</c> and <c>Split</c>
    /// </summary>
    /// <param name="member">resolved constructor or method</param>
    /// <param name="arguments">call-site argument values</param>
    /// <param name="receiver">receiver instance, or <c>null</c> for the <c>.ctor(char,int)</c> route</param>
    /// <param name="projectedBytes">bytes about to be allocated if this is a governed string operation</param>
    /// <returns>true if <paramref name="member"/> is a governed string operation</returns>
    static bool TryGetStringProjection(MethodBase member, object[] arguments, string receiver, out long projectedBytes) {
        projectedBytes = 0;
        ParameterInfo[] parameters = member.GetParameters();

        switch (member.Name) {
            case "PadRight":
            case "PadLeft":
                if (parameters.Length < 1 || parameters[0].ParameterType != typeof(int))
                    return false;
                projectedBytes = Convert.ToInt64(arguments[0]) * StringCharBytes;
                return true;

            case ".ctor":
                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(char) || parameters[1].ParameterType != typeof(int))
                    return false;
                projectedBytes = Convert.ToInt64(arguments[1]) * StringCharBytes;
                return true;

            case "Replace":
                if (receiver == null || parameters.Length < 2 || parameters[0].ParameterType != typeof(string) || parameters[1].ParameterType != typeof(string))
                    return false;
                string oldvalue = arguments[0] as string;
                if (string.IsNullOrEmpty(oldvalue))
                    return false;
                string newvalue = arguments[1] as string;
                projectedBytes = receiver.Length / Math.Max(1L, oldvalue.Length) * (newvalue?.Length ?? 0) * StringCharBytes;
                return true;

            case "ReplaceLineEndings":
                if (receiver == null || parameters.Length < 1 || parameters[0].ParameterType != typeof(string))
                    return false;
                string replacement = arguments[0] as string;
                projectedBytes = (long) receiver.Length * (replacement?.Length ?? 0) * StringCharBytes;
                return true;

            case "Split":
                if (receiver == null)
                    return false;
                projectedBytes = receiver.Length * SplitSegmentBytes;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// matches a resolved property against the pre-allocation operation table: the settable <c>Capacity</c>
    /// property on <see cref="List{T}"/> is the table's only property-set row
    /// </summary>
    /// <param name="receiverType">declaring or receiver type the property is set on</param>
    /// <param name="property">resolved property</param>
    /// <param name="bytesPerUnit">per-unit byte cost to charge if this is a capacity operation</param>
    /// <returns>true if <paramref name="property"/> is a capacity operation on a table-registered type</returns>
    internal static bool TryGetPreAllocationOperation(Type receiverType, PropertyInfo property, out long bytesPerUnit) {
        bytesPerUnit = 0;
        if (receiverType == null || property == null)
            return false;

        Type key = receiverType.IsGenericType ? receiverType.GetGenericTypeDefinition() : receiverType;
        if (key != typeof(List<>))
            return false;
        if (property.Name != "Capacity" || property.PropertyType != typeof(int))
            return false;

        bytesPerUnit = CollectionElementOverhead;
        return true;
    }
}

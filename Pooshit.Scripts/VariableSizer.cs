using System;
using System.Collections;

namespace Pooshit.Scripting;

/// <summary>
/// approximates the retained footprint of a script variable's value without reflection or unbounded traversal
/// </summary>
static class VariableSizer {

    const int MaxSizeDepth = 4;
    const long ObjectHeaderBytes = 24;
    const long ReferenceSize = 8;
    const long DictionaryEntryOverhead = 24;
    const long CollectionElementOverhead = 8;
    const long OpaqueValueBytes = 64;
    const long ValueTypeBytes = 24;

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
            return ObjectHeaderBytes + 2L * s.Length;

        switch (value) {
            case byte[] array: return ObjectHeaderBytes + array.Length;
            case sbyte[] array: return ObjectHeaderBytes + array.Length;
            case bool[] array: return ObjectHeaderBytes + array.Length;
            case char[] array: return ObjectHeaderBytes + 2L * array.Length;
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
        long size = ObjectHeaderBytes + dictionary.Count * DictionaryEntryOverhead;
        if (depth >= MaxSizeDepth)
            return size;

        foreach (DictionaryEntry entry in dictionary) {
            size += SizeCore(entry.Key, depth + 1, ref units);
            size += SizeCore(entry.Value, depth + 1, ref units);
        }
        return size;
    }

    static long SizeCollection(ICollection collection, int depth, ref long units) {
        long size = ObjectHeaderBytes + collection.Count * CollectionElementOverhead;
        if (depth >= MaxSizeDepth)
            return size;

        foreach (object element in collection)
            size += SizeCore(element, depth + 1, ref units);
        return size;
    }
}

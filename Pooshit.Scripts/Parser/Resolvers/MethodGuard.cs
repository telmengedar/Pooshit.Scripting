using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Pooshit.Scripting.Parser.Resolvers;

/// <summary>
/// governs which reflected members a script may dispatch to: an identity allow-list for the engine's own
/// reachable surface (tier 1), signature/shape rules for host-registered types (tier 2), and the
/// <c>ToString(format)</c> policy shared by both tiers
/// </summary>
public class MethodGuard {

    /// <summary>
    /// maximum length of an acceptable custom format string
    /// </summary>
    public const int MaxFormatLength = 64;

    /// <summary>
    /// maximum precision digit count of an acceptable standard format specifier
    /// </summary>
    public const int MaxPrecisionDigits = 2;

    static readonly Regex StandardFormatPattern = new("^[A-Za-z][0-9]*$", RegexOptions.Compiled);

    static readonly HashSet<string> PolicyMemberNames = new(StringComparer.Ordinal) {
        "padleft", "padright", "replace", "replacelineendings", "split", "tostring", "addrange", "insertrange", "ensurecapacity"
    };

    /// <summary>
    /// generated tier-1 inventory (design §7.3): for every type in round-8's reachable-receiver closure, the
    /// distinct lower-cased public-instance method names minus <see cref="PolicyMemberNames"/>. Produced by
    /// <c>Scripting.Tests/MethodGuardInventoryGenerator.cs</c>, re-run by <c>MethodGuardTests.T68</c> as a
    /// drift test against the runtime this assembly actually executes on
    /// </summary>
    static readonly Dictionary<Type, HashSet<string>> Tier1AllowedNames = new() {
        [typeof(bool)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(byte)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(char)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode"},
        [typeof(char[])] = new HashSet<string>(StringComparer.Ordinal) {"address", "clone", "copyto", "equals", "get", "get_isfixedsize", "get_isreadonly", "get_issynchronized", "get_length", "get_longlength", "get_rank", "get_syncroot", "getenumerator", "gethashcode", "getlength", "getlonglength", "getlowerbound", "gettype", "getupperbound", "getvalue", "initialize", "set", "setvalue"},
        [typeof(CharEnumerator)] = new HashSet<string>(StringComparer.Ordinal) {"clone", "dispose", "equals", "get_current", "gethashcode", "gettype", "movenext", "reset"},
        [typeof(DictionaryEntry)] = new HashSet<string>(StringComparer.Ordinal) {"deconstruct", "equals", "get_key", "get_value", "gethashcode", "gettype", "set_key", "set_value"},
        [typeof(Dictionary<,>)] = new HashSet<string>(StringComparer.Ordinal) {"add", "clear", "containskey", "containsvalue", "equals", "get_comparer", "get_count", "get_item", "get_keys", "get_values", "getenumerator", "gethashcode", "getobjectdata", "gettype", "ondeserialization", "remove", "set_item", "trimexcess", "tryadd", "trygetvalue"},
        [typeof(Dictionary<,>.Enumerator)] = new HashSet<string>(StringComparer.Ordinal) {"dispose", "equals", "get_current", "gethashcode", "gettype", "movenext"},
        [typeof(Dictionary<,>.KeyCollection)] = new HashSet<string>(StringComparer.Ordinal) {"contains", "copyto", "equals", "get_count", "getenumerator", "gethashcode", "gettype"},
        [typeof(Dictionary<,>.KeyCollection.Enumerator)] = new HashSet<string>(StringComparer.Ordinal) {"dispose", "equals", "get_current", "gethashcode", "gettype", "movenext"},
        [typeof(Dictionary<,>.ValueCollection)] = new HashSet<string>(StringComparer.Ordinal) {"copyto", "equals", "get_count", "getenumerator", "gethashcode", "gettype"},
        [typeof(Dictionary<,>.ValueCollection.Enumerator)] = new HashSet<string>(StringComparer.Ordinal) {"dispose", "equals", "get_current", "gethashcode", "gettype", "movenext"},
        [typeof(IEnumerator<>)] = new HashSet<string>(StringComparer.Ordinal) {"get_current"},
        [typeof(IEqualityComparer<>)] = new HashSet<string>(StringComparer.Ordinal) {"equals", "gethashcode"},
        [typeof(KeyValuePair<,>)] = new HashSet<string>(StringComparer.Ordinal) {"deconstruct", "equals", "get_key", "get_value", "gethashcode", "gettype"},
        [typeof(List<>)] = new HashSet<string>(StringComparer.Ordinal) {"add", "asreadonly", "binarysearch", "clear", "contains", "convertall", "copyto", "equals", "exists", "find", "findall", "findindex", "findlast", "findlastindex", "foreach", "get_capacity", "get_count", "get_item", "getenumerator", "gethashcode", "getrange", "gettype", "indexof", "insert", "lastindexof", "remove", "removeall", "removeat", "removerange", "reverse", "set_capacity", "set_item", "slice", "sort", "toarray", "trimexcess", "trueforall"},
        [typeof(List<>.Enumerator)] = new HashSet<string>(StringComparer.Ordinal) {"dispose", "equals", "get_current", "gethashcode", "gettype", "movenext"},
        [typeof(ICollection)] = new HashSet<string>(StringComparer.Ordinal) {"copyto", "get_count", "get_issynchronized", "get_syncroot"},
        [typeof(IDictionary)] = new HashSet<string>(StringComparer.Ordinal) {"add", "clear", "contains", "get_isfixedsize", "get_isreadonly", "get_item", "get_keys", "get_values", "getenumerator", "remove", "set_item"},
        [typeof(IDictionaryEnumerator)] = new HashSet<string>(StringComparer.Ordinal) {"get_entry", "get_key", "get_value"},
        [typeof(IEnumerator)] = new HashSet<string>(StringComparer.Ordinal) {"get_current", "movenext", "reset"},
        [typeof(ReadOnlyCollection<>)] = new HashSet<string>(StringComparer.Ordinal) {"contains", "copyto", "equals", "get_count", "get_item", "getenumerator", "gethashcode", "gettype", "indexof"},
        [typeof(decimal)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "get_scale", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(double)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(short)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(int)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(long)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(object)] = new HashSet<string>(StringComparer.Ordinal) {"equals", "gethashcode", "gettype"},
        [typeof(sbyte)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(float)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(string)] = new HashSet<string>(StringComparer.Ordinal) {"clone", "compareto", "contains", "copyto", "endswith", "enumeraterunes", "equals", "get_chars", "get_length", "getenumerator", "gethashcode", "getpinnablereference", "gettype", "gettypecode", "indexof", "indexofany", "insert", "isnormalized", "lastindexof", "lastindexofany", "normalize", "remove", "startswith", "substring", "tochararray", "tolower", "tolowerinvariant", "toupper", "toupperinvariant", "trim", "trimend", "trimstart", "trycopyto"},
        [typeof(string[])] = new HashSet<string>(StringComparer.Ordinal) {"address", "clone", "copyto", "equals", "get", "get_isfixedsize", "get_isreadonly", "get_issynchronized", "get_length", "get_longlength", "get_rank", "get_syncroot", "getenumerator", "gethashcode", "getlength", "getlonglength", "getlowerbound", "gettype", "getupperbound", "getvalue", "initialize", "set", "setvalue"},
#if NET8_0_OR_GREATER
        [typeof(System.Text.Rune)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "encodetoutf16", "encodetoutf8", "equals", "get_isascii", "get_isbmp", "get_plane", "get_utf16sequencelength", "get_utf8sequencelength", "get_value", "gethashcode", "gettype", "tryencodetoutf16", "tryencodetoutf8"},
        [typeof(System.Text.StringRuneEnumerator)] = new HashSet<string>(StringComparer.Ordinal) {"equals", "get_current", "getenumerator", "gethashcode", "gettype", "movenext"},
#endif
        [typeof(TypeCode)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "hasflag"},
        [typeof(ushort)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(uint)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
        [typeof(ulong)] = new HashSet<string>(StringComparer.Ordinal) {"compareto", "equals", "gethashcode", "gettype", "gettypecode", "tryformat"},
    };

    /// <summary>
    /// the checked-in tier-1 inventory, exposed for <c>MethodGuardTests.T68</c>'s drift comparison against a
    /// freshly re-run <c>Scripting.Tests/MethodGuardInventoryGenerator.cs</c>
    /// </summary>
    internal static IReadOnlyDictionary<Type, HashSet<string>> GeneratedInventory => Tier1AllowedNames;

    readonly Dictionary<Type, HashSet<string>> extraAllowedNames = new();
    readonly HashSet<Type> ungovernedTypes = new();
    readonly Dictionary<Type, HashSet<string>> deniedMembers = new();
    readonly Dictionary<(Type Type, string MethodName), (int ArgumentIndex, long BytesPerUnit)> chargedMembers = new();

    static Type Normalize(Type type) => type != null && type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    /// <summary>
    /// determines whether a receiver is subject to the <c>ToString(format)</c> policy: any type formattable
    /// through a public <c>ToString(string[,IFormatProvider])</c> overload (design §7.6.2)
    /// </summary>
    /// <param name="receiverType">runtime type of a dispatch receiver</param>
    /// <returns>true if the format policy applies to this receiver</returns>
    public static bool IsFormatFamily(Type receiverType) => receiverType != null && typeof(IFormattable).IsAssignableFrom(receiverType);

    /// <summary>
    /// determines whether a <c>ToString</c> format argument is small and simple enough to be affordable (design §9.1)
    /// </summary>
    /// <param name="format">runtime value of the format argument</param>
    /// <returns>true if the format is acceptable</returns>
    public static bool IsAcceptableFormat(string format) {
        if (string.IsNullOrEmpty(format))
            return true;
        if (format.Length > MaxFormatLength)
            return false;

        Match match = StandardFormatPattern.Match(format);
        return !match.Success || format.Length - 1 <= MaxPrecisionDigits;
    }

    /// <summary>
    /// determines whether <paramref name="receiverType"/> belongs to the engine's curated tier-1 closure
    /// </summary>
    /// <param name="receiverType">runtime type of a dispatch receiver</param>
    /// <returns>true if the engine hands this type's surface to script without a host opting in</returns>
    public bool IsTier1(Type receiverType) => Tier1AllowedNames.ContainsKey(Normalize(receiverType));

    /// <summary>
    /// determines whether a host removed all governance for a type via <see cref="Ungovern{T}"/>
    /// </summary>
    /// <param name="receiverType">runtime type of a dispatch receiver</param>
    /// <returns>true if the type's full public surface is reachable unconditionally</returns>
    public bool IsUngoverned(Type receiverType) => receiverType != null && ungovernedTypes.Contains(Normalize(receiverType));

    /// <summary>
    /// determines whether a <c>tostring</c> call's format argument must pass <see cref="IsAcceptableFormat"/>
    /// </summary>
    /// <param name="receiverType">runtime type of a dispatch receiver</param>
    /// <returns>true if the format policy applies and has not been removed via <see cref="Ungovern{T}"/></returns>
    public bool RequiresFormatCheck(Type receiverType) => IsFormatFamily(receiverType) && !IsUngoverned(receiverType);

    /// <summary>
    /// determines whether a resolved method may be dispatched to from script: tier-1 identity for the engine's
    /// own closure, tier-2 shape governance for everything else (design §7.6.6)
    /// </summary>
    /// <param name="hostType">runtime type of the dispatch receiver</param>
    /// <param name="method">resolved candidate method</param>
    /// <returns>true if the method passes governance</returns>
    public bool IsAllowed(Type hostType, MethodInfo method) {
        Type key = Normalize(hostType);
        if (ungovernedTypes.Contains(key))
            return true;

        string name = method.Name.ToLowerInvariant();
        if (!Tier1AllowedNames.TryGetValue(key, out HashSet<string> names))
            return IsPermittedByShape(key, name);

        if (PolicyMemberNames.Contains(name))
            return true;
        if (extraAllowedNames.TryGetValue(key, out HashSet<string> extra) && extra.Contains(name))
            return true;
        return names.Contains(name);
    }

    bool IsPermittedByShape(Type normalizedHostType, string loweredName) => !(deniedMembers.TryGetValue(normalizedHostType, out HashSet<string> denied) && denied.Contains(loweredName));

    /// <summary>
    /// widens a tier-1 type's generated allow inventory with additional method names
    /// </summary>
    /// <typeparam name="T">governed tier-1 type to widen</typeparam>
    /// <param name="methodNames">additional method names to allow</param>
    public void Allow<T>(params string[] methodNames) {
        Type key = Normalize(typeof(T));
        if (!extraAllowedNames.TryGetValue(key, out HashSet<string> names))
            extraAllowedNames[key] = names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string methodName in methodNames)
            names.Add(methodName.ToLowerInvariant());
    }

    /// <summary>
    /// removes all governance for a type: its full public surface becomes reachable unconditionally
    /// </summary>
    /// <typeparam name="T">type to stop governing</typeparam>
    public void Ungovern<T>() => ungovernedTypes.Add(Normalize(typeof(T)));

    /// <summary>
    /// registers a pre-allocation projection row for a size-parameterized member of a host-injected type
    /// </summary>
    /// <typeparam name="T">host-injected type the member is declared on</typeparam>
    /// <param name="methodName">name of the size-parameterized member</param>
    /// <param name="sizeArgumentIndex">index of the argument carrying the requested size</param>
    /// <param name="bytesPerUnit">bytes allocated per unit of the size argument</param>
    public void Charge<T>(string methodName, int sizeArgumentIndex, long bytesPerUnit) => chargedMembers[(Normalize(typeof(T)), methodName.ToLowerInvariant())] = (sizeArgumentIndex, bytesPerUnit);

    /// <summary>
    /// refuses specific members of a host-injected type outright
    /// </summary>
    /// <typeparam name="T">host-injected type to deny members of</typeparam>
    /// <param name="methodNames">method names to refuse</param>
    public void Deny<T>(params string[] methodNames) {
        Type key = Normalize(typeof(T));
        if (!deniedMembers.TryGetValue(key, out HashSet<string> names))
            deniedMembers[key] = names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string methodName in methodNames)
            names.Add(methodName.ToLowerInvariant());
    }

    /// <summary>
    /// projects the bytes a host-registered <see cref="Charge{T}"/> row is about to allocate
    /// </summary>
    /// <param name="hostType">runtime type of the dispatch receiver</param>
    /// <param name="member">resolved method</param>
    /// <param name="arguments">call-site argument values</param>
    /// <param name="projectedBytes">bytes about to be allocated if a host charge row matched</param>
    /// <returns>true if a host charge row matched <paramref name="member"/></returns>
    internal bool TryGetHostChargeProjection(Type hostType, MethodBase member, object[] arguments, out long projectedBytes) {
        projectedBytes = 0;
        if (!chargedMembers.TryGetValue((Normalize(hostType), member.Name.ToLowerInvariant()), out (int ArgumentIndex, long BytesPerUnit) row))
            return false;
        if (row.ArgumentIndex < 0 || arguments == null || row.ArgumentIndex >= arguments.Length)
            return false;

        projectedBytes = Convert.ToInt64(arguments[row.ArgumentIndex]) * row.BytesPerUnit;
        return true;
    }
}

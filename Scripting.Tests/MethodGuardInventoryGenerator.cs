using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pooshit.Scripting.Parser.Resolvers;

namespace Scripting.Tests;

/// <summary>
/// re-runs the design's §7.3 tier-1 closure/inventory derivation against the running runtime, so
/// <c>MethodGuardTests.T68_AllowListMatchesGeneratedInventory</c> can fail loudly the day a .NET upgrade
/// changes the reflected surface the checked-in <see cref="MethodGuard"/> table was generated from
/// </summary>
static class MethodGuardInventoryGenerator {

    /// <summary>
    /// seeds for the closure walk: the <c>Types.AddType</c> registrations (<c>Pooshit.Scripts/Parser/ScriptParser.cs</c>)
    /// plus <see cref="Dictionary{TKey,TValue}"/> itself, since a <c>{ }</c> dictionary literal's runtime
    /// receiver is the concrete <c>Dictionary&lt;object,object&gt;</c> (<see cref="Pooshit.Scripting.Tokens.DictionaryToken.Execute"/>),
    /// not the <see cref="System.Collections.IDictionary"/> interface script registers the type name under
    /// </summary>
    static readonly Type[] Seeds = {
        typeof(List<object>), typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(string),
        typeof(short), typeof(int), typeof(ushort), typeof(uint), typeof(ulong), typeof(long),
        typeof(float), typeof(double), typeof(decimal), typeof(object), typeof(System.Collections.IDictionary),
        typeof(Dictionary<object, object>)
    };

    /// <summary>
    /// member names carrying pre-allocation or format policy instead of a blanket identity allow (design §7.3/§7.4/§9.1)
    /// </summary>
    internal static readonly HashSet<string> PolicyMemberNames = new(StringComparer.Ordinal) {
        "padleft", "padright", "replace", "replacelineendings", "split", "tostring", "addrange", "insertrange", "ensurecapacity"
    };

    static Type StripIndirection(Type type) => type != null && (type.IsByRef || type.IsPointer) ? StripIndirection(type.GetElementType()) : type;

    static Type NormalizeGeneric(Type type) => type != null && type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;

    /// <summary>
    /// determines whether <paramref name="type"/> is, or is an array of, an unbound generic parameter - true
    /// for <c>T[]</c> reflected off an open generic type definition (eg. walking <c>List&lt;&gt;</c> itself
    /// reaches <c>ToArray()</c>'s <c>T[]</c> return type), which is not a real receiver and must not enter the
    /// closure even though the array type itself is not, by <see cref="Type.IsGenericParameter"/>, one
    /// </summary>
    /// <param name="type">type to test</param>
    /// <returns>true if the type is unbound</returns>
    static bool IsUnboundGenericShape(Type type) => type != null && (type.IsGenericParameter || (type.IsArray && IsUnboundGenericShape(type.GetElementType())));

    /// <summary>
    /// expands a raw reflected type into every type the closure walk must consider: the array type itself -
    /// an engine-surface receiver in its own right, since <c>"abc".tochararray()</c>/<c>"a,b".split(",")</c>
    /// hand script a live array - followed by its element type, so reachability continues past the array
    /// </summary>
    /// <param name="rawType">a method return type, property type or field type as reflected</param>
    /// <returns>every type this raw type contributes to the closure walk</returns>
    static IEnumerable<Type> Expand(Type rawType) {
        Type type = StripIndirection(rawType);
        if (type == null)
            yield break;

        if (type.IsArray) {
            if (!IsUnboundGenericShape(type))
                yield return type;
            foreach (Type elementType in Expand(type.GetElementType()))
                yield return elementType;
            yield break;
        }

        yield return NormalizeGeneric(type);
    }

    static bool IsEligible(Type type) => type != null && type != typeof(void) && !type.IsGenericParameter && !TypeGuard.IsForbiddenReflectiveReceiver(type);

    /// <summary>
    /// computes the tier-1 reachable-receiver closure: the seeds, transitively closed over public-instance
    /// method return types, property types and field types - including array types themselves, not just
    /// their element types - pruning <see cref="TypeGuard"/>'s deny-family
    /// </summary>
    /// <returns>every type reachable from the seeds without a host opting in</returns>
    internal static HashSet<Type> ComputeClosure() {
        HashSet<Type> closure = new();
        Queue<Type> pending = new(Seeds.SelectMany(Expand));

        while (pending.Count > 0) {
            Type type = pending.Dequeue();
            if (type == null || !IsEligible(type) || !closure.Add(type))
                continue;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                if (method.IsGenericMethodDefinition || method.IsSpecialName)
                    continue;
                foreach (Type returnType in Expand(method.ReturnType))
                    if (IsEligible(returnType) && !closure.Contains(returnType))
                        pending.Enqueue(returnType);
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                foreach (Type propertyType in Expand(property.PropertyType))
                    if (IsEligible(propertyType) && !closure.Contains(propertyType))
                        pending.Enqueue(propertyType);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                foreach (Type fieldType in Expand(field.FieldType))
                    if (IsEligible(fieldType) && !closure.Contains(fieldType))
                        pending.Enqueue(fieldType);
            }
        }

        return closure;
    }

    /// <summary>
    /// computes, for every type in <see cref="ComputeClosure"/>, the distinct lower-cased public-instance
    /// method names minus <see cref="PolicyMemberNames"/> - the literal data checked into <c>MethodGuard.cs</c>
    /// </summary>
    /// <returns>per-type curated allow-name sets</returns>
    internal static Dictionary<Type, HashSet<string>> ComputeInventory() {
        Dictionary<Type, HashSet<string>> inventory = new();
        foreach (Type type in ComputeClosure()) {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                string name = method.Name.ToLowerInvariant();
                if (!PolicyMemberNames.Contains(name))
                    names.Add(name);
            }

            inventory[type] = names;
        }

        return inventory;
    }
}

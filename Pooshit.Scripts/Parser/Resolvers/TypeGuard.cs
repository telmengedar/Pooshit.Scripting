using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pooshit.Scripting.Parser.Resolvers;

/// <summary>
/// refuses reflective access to the type-system itself from script
/// </summary>
public static class TypeGuard {
    static readonly Type[] denyFamily = [
        typeof(Type),
        typeof(MemberInfo),
        typeof(ParameterInfo),
        typeof(Assembly),
        typeof(Module),
        typeof(Activator),
        typeof(AppDomain),
        typeof(RuntimeTypeHandle),
        typeof(RuntimeMethodHandle),
        typeof(RuntimeFieldHandle)
    ];

    static readonly HashSet<string> forbiddenMethodNames = new(StringComparer.Ordinal) {
        "invokemember", "getmethod", "getmethods", "getconstructor", "getconstructors",
        "getmember", "getmembers", "getfield", "getfields", "getproperty", "getproperties",
        "makegenerictype", "makearraytype", "dynamicinvoke", "createinstance",
        "getinterface", "getinterfaces", "getnestedtype", "getruntimemethod"
    };

    /// <summary>
    /// determines whether a receiver type belongs to the reflection type-family
    /// </summary>
    /// <param name="receiverType">runtime or static type of a dispatch receiver</param>
    /// <returns>true if dispatching against this receiver must be refused</returns>
    public static bool IsForbiddenReflectiveReceiver(Type receiverType) {
        if (receiverType == null)
            return false;

        foreach (Type denied in denyFamily) {
            if (denied.IsAssignableFrom(receiverType))
                return true;
        }

        return false;
    }

    /// <summary>
    /// determines whether a method name is a reflection entry point, regardless of receiver
    /// </summary>
    /// <param name="loweredName">lower-cased method name</param>
    /// <returns>true if the method name must be refused</returns>
    public static bool IsForbiddenReflectiveMethodName(string loweredName) {
        return loweredName != null && forbiddenMethodNames.Contains(loweredName);
    }
}

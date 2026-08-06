using System;

namespace Pooshit.Scripting.Data;

/// <summary>
/// opaque handle for a type, exposing identity only; never exposes a reflective surface to script
/// </summary>
public sealed class ScriptType : IEquatable<ScriptType> {
    readonly Type type;

    ScriptType(Type type) {
        this.type = type;
    }

    /// <summary>
    /// wraps a type, preserving null-propagation for a null argument
    /// </summary>
    /// <param name="type">type to wrap</param>
    /// <returns>wrapped handle, or null if <paramref name="type"/> is null</returns>
    internal static ScriptType Of(Type type) {
        return type == null ? null : new ScriptType(type);
    }

    /// <summary>
    /// unwraps the wrapped type; internal so script code can never reach it
    /// </summary>
    /// <returns>wrapped type</returns>
    internal Type Unwrap() => type;

    /// <summary>
    /// simple name of the wrapped type
    /// </summary>
    public string Name => type.Name;

    /// <summary>
    /// full name of the wrapped type
    /// </summary>
    public string FullName => type.FullName;

    /// <inheritdoc />
    public bool Equals(ScriptType other) {
        if (other is null)
            return false;
        return ReferenceEquals(this, other) || type == other.type;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => Equals(obj as ScriptType);

    /// <inheritdoc />
    public override int GetHashCode() => type.GetHashCode();

    /// <summary>
    /// compares two handles by wrapped type identity
    /// </summary>
    public static bool operator ==(ScriptType lhs, ScriptType rhs) {
        if (lhs is null)
            return rhs is null;
        return lhs.Equals(rhs);
    }

    /// <summary>
    /// compares two handles by wrapped type identity
    /// </summary>
    public static bool operator !=(ScriptType lhs, ScriptType rhs) => !(lhs == rhs);

    /// <inheritdoc />
    public override string ToString() => FullName ?? Name;
}

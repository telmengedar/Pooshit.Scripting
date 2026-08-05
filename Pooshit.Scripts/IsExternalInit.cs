#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices {

    /// <summary>
    /// compiler-recognised marker type that enables C# <c>init</c>-only property accessors. Part of the
    /// BCL from .NET 5 / C# 9 onward; <c>netstandard2.0</c> predates it, so the compiler requires this
    /// exact type to exist somewhere in the compilation closure before it will emit <c>init</c> accessors
    /// targeting that framework. Never referenced directly by application code
    /// </summary>
    static class IsExternalInit { }
}
#endif

using System.Runtime.CompilerServices;

/// <summary>
/// grants the test project access to internal members needed to assert execution-guard invariants directly
/// </summary>
[assembly: InternalsVisibleTo("Scripting.Tests")]

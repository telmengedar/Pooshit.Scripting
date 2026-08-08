using System.Runtime.CompilerServices;

/// <summary>
/// grants the test project access to internal members needed to assert CLI option-parsing behavior directly
/// </summary>
[assembly: InternalsVisibleTo("Scripting.Tests")]

using System;

namespace Pooshit.Scripting;

/// <summary>
/// execution guards a host can apply to bound a script's runtime; every knob is opt-in and defaults to
/// <c>null</c>, which reproduces the engine's unrestricted, pre-existing behavior.
/// </summary>
/// <remarks>
/// Immutable by design (§8 of docs/architecture/cancellation-support.md): <see cref="None"/> is a single
/// shared instance handed out as the default on every <see cref="Parser.ScriptParser.Limits"/> and
/// <see cref="ScriptContext.Limits"/>. If the knobs were mutable, <c>parser.Limits.Timeout = x</c> would
/// silently impose that timeout on every other default-configured parser sharing the same <see cref="None"/>
/// reference in the process — exactly the mixed-host process (some trusted, some sandboxed) this feature
/// exists to serve. A host that wants limits constructs its own instance
/// (<c>new ScriptLimits { Timeout = x }</c>) and assigns it to <c>parser.Limits</c> instead of mutating the
/// default in place.
/// </remarks>
public class ScriptLimits {

    /// <summary>
    /// shared instance representing no configured limits (today's unrestricted behavior); used as the
    /// default so consumers never need to null-check <see cref="ScriptContext.Limits"/>. Safe to share
    /// because every knob is <c>init</c>-only and this instance never sets any of them
    /// </summary>
    public static readonly ScriptLimits None = new();

    /// <summary>
    /// <see cref="MaxDepth"/> value used by <see cref="Default"/>
    /// </summary>
    public const int DefaultMaxDepth = 10;

    /// <summary>
    /// <see cref="MaxVariableBytes"/> value used by <see cref="Default"/>
    /// </summary>
    public const long DefaultMaxVariableBytes = 128L * 1024 * 1024;

    /// <summary>
    /// <see cref="MaxParseDepth"/> value used by <see cref="Default"/>
    /// </summary>
    public const int DefaultMaxParseDepth = 100;

    /// <summary>
    /// <see cref="RegexTimeout"/> value used by <see cref="Default"/>
    /// </summary>
    public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// <see cref="ParseTimeout"/> value used by <see cref="Default"/>
    /// </summary>
    public static readonly TimeSpan DefaultParseTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// shared instance bounding call depth, parse-recursion depth, parse wall-clock time and variable
    /// footprint, leaving every other knob unset; the default assigned to <see cref="Parser.ScriptParser.Limits"/>.
    /// Assign <see cref="None"/> instead to opt out of every bound
    /// </summary>
    public static readonly ScriptLimits Default = new() {MaxDepth = DefaultMaxDepth, MaxParseDepth = DefaultMaxParseDepth, MaxVariableBytes = DefaultMaxVariableBytes, RegexTimeout = DefaultRegexTimeout, ParseTimeout = DefaultParseTimeout};

    /// <summary>
    /// wall-clock deadline for a single script execution, or <c>null</c> to allow unbounded execution time
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// maximum number of engine checkpoints (statements, loop iterations, lambda invocations, enumerated
    /// elements) a script may execute before it is aborted, or <c>null</c> for no step budget
    /// </summary>
    public long? MaxSteps { get; init; }

    /// <summary>
    /// maximum duration a single regex match (<c>~~</c>/<c>!~</c>) may run, or <c>null</c> for unbounded
    /// matching; a breach surfaces as the regex engine's own
    /// <see cref="System.Text.RegularExpressions.RegexMatchTimeoutException"/>
    /// </summary>
    public TimeSpan? RegexTimeout { get; init; }

    /// <summary>
    /// maximum call depth a script may reach through lambda invocation and imported-script invocation before
    /// it is aborted, or <c>null</c> for no depth ceiling (today's behavior); guards against an uncatchable
    /// <see cref="StackOverflowException"/>
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// maximum recursion depth the parser may reach while parsing nested constructs (parenthesized
    /// expressions, statement/dictionary blocks, nested method calls), or <c>null</c> for no ceiling; guards
    /// against an uncatchable <see cref="StackOverflowException"/> raised at parse time. Distinct from
    /// <see cref="MaxDepth"/>, which bounds runtime call depth
    /// </summary>
    public int? MaxParseDepth { get; init; }

    /// <summary>
    /// wall-clock deadline for a single <see cref="Parser.ScriptParser.Parse(string)"/> call, or <c>null</c>
    /// for unbounded parse time (today's behavior for a host that opts out via <see cref="None"/>); guards
    /// against a non-terminating parse (DiVoid #9341) that <see cref="MaxParseDepth"/> cannot catch, because
    /// that knob bounds recursion depth while a non-terminating parse can instead be an unbounded loop that
    /// keeps calling back into the parser at a constant depth. Checked in <see cref="Parser.ScriptParser.Parse(IScriptToken,ref string,ref int,ref int,ref int,bool,bool)"/>
    /// on every recursive descent, i.e. every construct the parser re-enters through - so it backstops the
    /// whole class of parse-time hangs rather than one specific construct
    /// </summary>
    public TimeSpan? ParseTimeout { get; init; }

    /// <summary>
    /// maximum number of live variable entries a script may hold before it is aborted, or <c>null</c> for no entry-count ceiling
    /// </summary>
    public long? MaxVariables { get; init; }

    /// <summary>
    /// maximum approximated footprint in bytes of a script's own variables before it is aborted, or <c>null</c> for no footprint ceiling
    /// </summary>
    public long? MaxVariableBytes { get; init; }
}

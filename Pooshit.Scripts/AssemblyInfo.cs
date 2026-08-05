using System.Runtime.CompilerServices;

/// <summary>
/// grants the test project access to internal members (eg. <c>ScriptContext.DepthBudget</c>,
/// <c>ScriptContext.StepBudget</c>, <c>GuardedExecution</c>) so backward-compatibility invariants like "no
/// budget object exists when its knob is unconfigured" (design §12 S2) can be asserted directly rather than
/// only inferred from behavior. This is the only production-assembly accessibility change in the depth-guard
/// PR; it exposes nothing to any consumer other than the test assembly named here
/// </summary>
[assembly: InternalsVisibleTo("Scripting.Tests")]

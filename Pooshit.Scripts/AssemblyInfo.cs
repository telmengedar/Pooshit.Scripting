using System.Runtime.CompilerServices;

/// <summary>
/// grants the test project access to internal members (eg. <c>ScriptContext.DepthBudget</c>,
/// <c>ScriptContext.StepBudget</c>, <c>GuardedExecution</c>) so invariants like "no budget object exists
/// when its knob is unconfigured" can be asserted directly rather than only inferred from behavior. This is
/// the only production-assembly accessibility relaxation this repo makes; it exposes nothing to any consumer
/// other than the test assembly named here
/// </summary>
[assembly: InternalsVisibleTo("Scripting.Tests")]

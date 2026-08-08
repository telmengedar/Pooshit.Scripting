using System;
using System.IO;
using NightlyCode.ScriptExecutor;
using NUnit.Framework;
using Pooshit.Scripting;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="Program.TryParseOptions"/> and <see cref="Program.PrintUsage"/> (DiVoid #7887): the
    /// CLI's override path built a <see cref="ScriptLimits"/> instance by hand, which silently dropped every
    /// knob it did not restate, and its usage text hardcoded a stale default for <c>--regex-timeout</c>
    /// </summary>
    [TestFixture, Parallelizable]
    public class ScriptExecutorOptionParsingTests {

        [Test, Parallelizable]
        [Description("DiVoid #7887: an unrelated flag (--timeout) must not drop MaxParseDepth to null - the parse-depth guard has to survive any single-knob override.")]
        public void UnrelatedFlag_LeavesMaxParseDepthAtDefault() {
            bool parsed = Program.TryParseOptions(new[] {"--timeout", "5000", "script.ns"}, out ScriptLimits limits, out _, out _);

            Assert.That(parsed, Is.True);
            Assert.That(limits.MaxParseDepth, Is.EqualTo(ScriptLimits.DefaultMaxParseDepth));
        }

        [Test, Parallelizable]
        [Description("DiVoid #7887: a single-knob override composes onto ScriptLimits.Default rather than replacing it - every other knob must retain its default value.")]
        public void UnrelatedFlag_LeavesEveryOtherKnobAtDefault() {
            Program.TryParseOptions(new[] {"--timeout", "5000", "script.ns"}, out ScriptLimits limits, out _, out _);

            Assert.That(limits.MaxDepth, Is.EqualTo(ScriptLimits.DefaultMaxDepth));
            Assert.That(limits.MaxVariableBytes, Is.EqualTo(ScriptLimits.DefaultMaxVariableBytes));
            Assert.That(limits.RegexTimeout, Is.EqualTo(ScriptLimits.DefaultRegexTimeout));
            Assert.That(limits.MaxSteps, Is.Null);
            Assert.That(limits.MaxVariables, Is.Null);
            Assert.That(limits.Timeout, Is.EqualTo(TimeSpan.FromMilliseconds(5000)));
        }

        [Test, Parallelizable]
        public void NoFlags_ReturnsScriptLimitsDefaultInstanceUnchanged() {
            Program.TryParseOptions(new[] {"script.ns"}, out ScriptLimits limits, out _, out _);

            Assert.That(limits, Is.SameAs(ScriptLimits.Default));
        }

        [Test, Parallelizable]
        [Description("DiVoid #7887: --unbounded resets every knob to null, but a flag stacked alongside it must still bind that one knob - proving absent-flag inheritance and explicit-flag override compose correctly against either baseline.")]
        public void UnboundedWithExplicitOverride_OnlyOverriddenKnobIsBound() {
            Program.TryParseOptions(new[] {"--unbounded", "--timeout", "5000", "script.ns"}, out ScriptLimits limits, out _, out _);

            Assert.That(limits.Timeout, Is.EqualTo(TimeSpan.FromMilliseconds(5000)));
            Assert.That(limits.MaxParseDepth, Is.Null);
            Assert.That(limits.MaxDepth, Is.Null);
            Assert.That(limits.MaxVariableBytes, Is.Null);
            Assert.That(limits.RegexTimeout, Is.Null);
        }

        [Test, Parallelizable]
        [Description("DiVoid #7887: --regex-timeout usage text must state the actual configured default (1000ms since PR #18), not the stale '(default unset)'.")]
        public void PrintUsage_RegexTimeoutLine_StatesConfiguredDefault() {
            StringWriter writer = new();
            Program.PrintUsage(writer);

            string output = writer.ToString();
            Assert.That(output, Does.Contain($"override RegexTimeout in milliseconds (default {(int) ScriptLimits.DefaultRegexTimeout.TotalMilliseconds})"));
            Assert.That(output, Does.Not.Contain("RegexTimeout in milliseconds (default unset)"));
        }
    }
}

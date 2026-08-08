using System.Linq;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extensions.Script;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises the secure-by-default flip (docs/architecture/execution-guards-depth-memory.md §18):
    /// <see cref="ScriptLimits.Default"/> as the new <see cref="ScriptParser.Limits"/> initial value, and
    /// <see cref="ScriptLimits.None"/> as the intentional boundless opt-out
    /// </summary>
    [TestFixture, Parallelizable]
    public class SecureByDefaultTests {

        static IScript ParseRecursiveFactorial(ScriptParser parser) {
            return parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac.invoke(1000000)"
            ));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T34: a default-constructed ScriptParser aborts a recursion deeper than DefaultMaxDepth without any Limits configured.")]
        public void T34_DefaultParserAbortsRecursionPastDefaultMaxDepth() {
            ScriptParser parser = new();
            IScript script = ParseRecursiveFactorial(parser);

            ScriptDepthLimitExceededException exception = Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
            Assert.That(exception.Limit, Is.EqualTo(ScriptLimits.DefaultMaxDepth));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T35: a shallow, sequential iterative transform over a large collection does not consume the default depth budget, since .where callbacks are sequential rather than nested.")]
        public void T35_DefaultParserCompletesShallowIterativeTransform() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.where($x=>$x>0).count()");

            int[] source = Enumerable.Range(-5000, 10000).ToArray();
            int result = script.Execute<int>(new VariableProvider(new Variable("src", source)));

            Assert.That(result, Is.EqualTo(source.Count(x => x > 0)));
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("T36: a default-constructed ScriptParser aborts a script growing its own variable footprint past DefaultMaxVariableBytes without any Limits configured.")]
        public void T36_DefaultParserAbortsVariableGrowthPastDefaultMaxVariableBytes() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "while(true) {",
                "  $l.add(\"" + new string('x', 100_000) + "\")",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T37: assigning ScriptLimits.None restores the pre-flip boundless behavior, so a bounded (not truly unbounded) recursion past DefaultMaxDepth does not abort.")]
        public void T37_NoneOptOutRestoresUnboundedRecursion() {
            ScriptParser parser = new() {Limits = ScriptLimits.None};
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac.invoke(" + (ScriptLimits.DefaultMaxDepth + 5) + ")"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(10000)]
        [Description("T40 (DiVoid #7836/#7837): the round-4 red-team's exact reproduction - new list(100000000) assigned and held on a bare default-constructed ScriptParser - must now abort under DefaultMaxVariableBytes instead of allocating its ~800MB backing array silently.")]
        public void T40_DefaultParserAbortsHeldMegaPreSizedList() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$a = new list(100000000)");

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test]
        [Description("T38: ScriptLimits.Default pins the three secured knobs (DiVoid #7869 MaxParseDepth alongside the original MaxDepth/MaxVariableBytes) and leaves every other knob unset.")]
        public void T38_DefaultInstanceValues() {
            Assert.That(ScriptLimits.Default.MaxDepth, Is.EqualTo(ScriptLimits.DefaultMaxDepth));
            Assert.That(ScriptLimits.Default.MaxVariableBytes, Is.EqualTo(ScriptLimits.DefaultMaxVariableBytes));
            Assert.That(ScriptLimits.Default.MaxParseDepth, Is.EqualTo(ScriptLimits.DefaultMaxParseDepth));
            Assert.That(ScriptLimits.Default.MaxSteps, Is.Null);
            Assert.That(ScriptLimits.Default.Timeout, Is.Null);
            Assert.That(ScriptLimits.Default.RegexTimeout, Is.Null);
            Assert.That(ScriptLimits.Default.MaxVariables, Is.Null);
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T39: ScriptLimits.Default is a safe shared instance under init-only knobs - repointing one parser to None must not affect another default-configured parser.")]
        public void T39_DefaultInstanceIsSafeSharedAcrossParsers() {
            ScriptParser optedOutParser = new() {Limits = ScriptLimits.None};
            ScriptParser defaultParser = new();

            Assert.That(optedOutParser.Limits, Is.SameAs(ScriptLimits.None));
            Assert.That(defaultParser.Limits, Is.SameAs(ScriptLimits.Default));

            IScript script = ParseRecursiveFactorial(defaultParser);
            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }
    }
}

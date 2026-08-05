using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extensions.Script;
using Pooshit.Scripting.Hosts;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Providers;

namespace Scripting.Tests {

    /// <summary>
    /// exercises the recursion-depth guard (docs/architecture/execution-guards-depth-memory.md, phases 1-3):
    /// <see cref="ScriptLimits.MaxDepth"/>, the <see cref="ScriptAbortException"/> base that re-parents
    /// <see cref="ScriptStepLimitExceededException"/> and <see cref="ScriptTimeoutException"/>, and the
    /// two-clause passthrough shape at every dispatch wrapper. Every unbounded-script test carries a
    /// <see cref="MaxTimeAttribute"/>, so a pass is evidence of the guard firing rather than of a script that
    /// merely happened to finish
    /// </summary>
    [TestFixture, Parallelizable]
    public class ExecutionGuardTests {

        /// <summary>
        /// import provider returning a fixed, pre-built external method regardless of the requested key
        /// </summary>
        class FixedImportProvider : IImportProvider {
            readonly object result;

            public FixedImportProvider(object result) {
                this.result = result;
            }

            public object Import(object[] parameters) => result;
        }

        /// <summary>
        /// import provider whose target can be assigned after construction, used to wire up a mutually
        /// recursive pair of scripts that would otherwise need to reference each other before either exists
        /// </summary>
        class SettableImportProvider : IImportProvider {
            public object Target { get; set; }

            public object Import(object[] parameters) => Target;
        }

        /// <summary>
        /// reflected host method that calls back into a script lambda; used to exercise the
        /// <see cref="System.Reflection.TargetInvocationException"/> passthrough at
        /// <c>Operations/MethodOperations.cs:292</c>. Since DiVoid #7749, <c>$lambda.invoke()</c> itself no
        /// longer goes through reflection (<see cref="LambdaMethod"/> implements <see cref="IExternalMethod"/>,
        /// so <c>ScriptMethod</c>'s fast path calls it directly) — this method is the one remaining call shape
        /// in this file that genuinely reflects, exercising the filter on the way <em>out</em> as a breach
        /// deep in <paramref name="callback"/>'s own recursion propagates back through this one reflected frame
        /// </summary>
        /// <param name="callback">lambda to invoke</param>
        /// <param name="n">argument passed to the lambda</param>
        /// <returns>result of the callback invocation</returns>
        public static object InvokeCallback(LambdaMethod callback, int n) => callback.Invoke(n);

        static IScript ParseRecursiveFactorial(ScriptParser parser, string invocation) {
            return parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                invocation
            ));
        }

        /// <summary>
        /// safe recursion ceiling for tests in this file. Originally measured against
        /// <c>$lambda.invoke()</c> recursion when that call was reflected on every level: empirically, this
        /// interpreter's per-level stack cost through the reflected path was far higher than the "~10
        /// physical frames per call" the design's §11 sizing guidance assumed — a depth ceiling around 20 on
        /// that test host's default thread stack already risked a genuine, uncatchable
        /// <see cref="System.StackOverflowException"/> while <em>unwinding</em> the breach through as many
        /// stacked reflection/exception-filter frames, well before the "low hundreds" the design floated.
        /// Since DiVoid #7749, <c>$lambda.invoke()</c> no longer reflects (see <see cref="LambdaMethod"/>'s
        /// <see cref="IExternalMethod"/> implementation), which likely raises the real safe ceiling for that
        /// specific call shape — not re-measured, since 8 remains conservatively safe either way and every
        /// depth used below stays comfortably under it. <c>InvokeCallback</c>-shaped recursion (still
        /// reflected) is the case this value must still stay safe for. This remains a real finding for
        /// whoever sizes <see cref="ScriptLimits.MaxDepth"/> operationally (eg. Uberkarl, #7407) and does not
        /// indicate a defect in the guard itself — the guard fires at exactly the configured ceiling every
        /// time; only the margin between "configured ceiling" and "physical stack" is narrower than assumed
        /// for calls that go through reflection, and CF-3's measurement (dropped) confirmed no engine
        /// mechanism watches that margin.
        /// </summary>
        const int SafeMaxDepth = 8;

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_RecursiveLambdaExceedsMaxDepthThrows() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "$fac.invoke(1000000)");

            ScriptDepthLimitExceededException exception = Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
            Assert.That(exception.Limit, Is.EqualTo(SafeMaxDepth));
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_RecursiveLambdaWithoutMaxDepthUnchangedBehavior() {
            ScriptParser parser = new();
            IScript script = ParseRecursiveFactorial(parser, "$fac.invoke(" + SafeMaxDepth + ")");

            Assert.AreEqual(0, script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_NestedBlocksWithoutCallsDoNotCountTowardDepth() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };

            List<string> lines = new();
            for (int i = 0; i < 40; i++)
                lines.Add("if(true) {");
            lines.Add("$x = 1");
            for (int i = 0; i < 40; i++)
                lines.Add("}");

            IScript script = parser.Parse(ScriptCode.Create(lines.ToArray()));

            Assert.AreEqual(1, script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_BoundedRecursionUnderLimitCompletesNormally() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "$fac.invoke(" + (SafeMaxDepth - 1) + ")");

            Assert.AreEqual(0, script.Execute());
        }

        /// <summary>
        /// pins the <see cref="DepthBudget"/> enter/exit pairing: 1000 repetitions of a recursion that
        /// unwinds via a script-level <c>throw</c> caught by the loop's own <c>try</c>/<c>catch</c> must not
        /// leave any residual depth behind, or a subsequent, entirely legal call would spuriously breach
        /// </summary>
        [Test, Parallelizable, MaxTime(5000)]
        public void Depth_ThrowUnwindRepeatedInLoopDoesNotLeakDepth() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n==3) {",
                "    throw(\"boom\")",
                "  }",
                "  return($fac.invoke($n+1))",
                "}",
                "for($i=0,$i<1000,++$i) {",
                "  try {",
                "    $fac.invoke(0)",
                "  } catch {",
                "    $caught = true",
                "  }",
                "}",
                "$verify = $n=>{",
                "  if($n>0) {",
                "    return($verify.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$verify.invoke(4)"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_MutuallyRecursiveImportChainThrows() {
            SettableImportProvider providerA = new();
            SettableImportProvider providerB = new();

            ScriptParser parserA = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth},
                ImportProvider = providerA
            };
            IScript scriptA = parserA.Parse(ScriptCode.Create(
                "$m = import(\"b\")",
                "$m.invoke()"
            ));

            ScriptParser parserB = new() {
                ImportProvider = providerB
            };
            IScript scriptB = parserB.Parse(ScriptCode.Create(
                "$m = import(\"a\")",
                "$m.invoke()"
            ));

            providerA.Target = new ExternalScriptMethod(scriptB);
            providerB.Target = new ExternalScriptMethod(scriptA);

            Assert.Throws<ScriptDepthLimitExceededException>(() => scriptA.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_ImportedScriptOwnStepBudgetStillIndependent() {
            ScriptParser innerParser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
            };
            IScript imported = innerParser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));

            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = 1000},
                ImportProvider = new FixedImportProvider(new ExternalScriptMethod(imported))
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$m = import(\"whatever\")",
                "$m()"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// synchronisation gate that releases only once every participant has arrived, used to make N
        /// concurrent invocations provably simultaneously "in flight" rather than depending on scheduling luck
        /// to reproduce DiVoid #7744 CF-1/CF-5 (concurrency counted as nesting) deterministically
        /// </summary>
        class SyncGate {
            readonly Barrier barrier;
            public SyncGate(int participants) => barrier = new Barrier(participants);
            public object Arrive() {
                barrier.SignalAndWait(TimeSpan.FromSeconds(4));
                return null;
            }
        }

        /// <summary>
        /// drives <paramref name="count"/> invocations of <see cref="LambdaMethod.InvokeOnNewStack"/> from that
        /// many dedicated, test-owned <see cref="Thread"/> instances rather than the .NET thread pool (DiVoid
        /// #7744/#7749 W-G). The prior <c>task.run</c>/<c>Task.Run</c>-based version of these tests needed N
        /// pool threads available at essentially the same moment, which a starved or throttled CI runner does
        /// not guarantee, and separately raced NUnit's own <see cref="MaxTimeAttribute"/> against
        /// <see cref="SyncGate"/>'s internal wait timeout with the wrong ordering. A dedicated
        /// <see cref="Thread"/> starts immediately regardless of pool state, so the only synchronisation left
        /// is the in-script <see cref="SyncGate"/> itself — this still exercises the exact production method
        /// <see cref="Hosts.TaskHost.Run"/> calls, just without depending on a scheduler to actually run it
        /// concurrently
        /// </summary>
        static void RunConcurrentInvokeOnNewStack(LambdaMethod lambda, int count) {
            Exception[] exceptions = new Exception[count];
            Thread[] threads = new Thread[count];
            for (int i = 0; i < count; i++) {
                int index = i;
                threads[i] = new Thread(() => {
                    try {
                        lambda.InvokeOnNewStack();
                    }
                    catch (Exception e) {
                        exceptions[index] = e;
                    }
                });
            }

            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Exception first = exceptions.FirstOrDefault(e => e != null);
            if (first != null)
                throw first;
        }

        /// <summary>
        /// DiVoid #7744 CF-1: <c>N &gt; MaxDepth</c> concurrent, non-recursive lambda invocations, each on its
        /// own physical stack, must not breach a ceiling none of them individually approaches. Sized at the
        /// exact boundary QA measured (<c>N = MaxDepth + 1</c>) with a <see cref="SyncGate"/> forcing true
        /// simultaneity, so this test fails deterministically against the pre-fix behaviour (where every
        /// invocation entered the same shared <see cref="DepthBudget"/>) rather than passing by accident
        /// because <c>MaxDepth</c> was sized with headroom to spare
        /// </summary>
        [Test, Parallelizable, MaxTime(5000)]
        public void Depth_ConcurrentTaskRunLambdasDoNotSpuriouslyBreach() {
            const int taskCount = SafeMaxDepth + 1;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$wrapper = []=>{ $gate.Arrive() }",
                "$wrapper"
            ));

            SyncGate gate = new(taskCount);
            VariableProvider variables = new(new Variable("gate", gate));
            LambdaMethod wrapper = (LambdaMethod) definitions.Execute(variables);

            Assert.DoesNotThrow(() => RunConcurrentInvokeOnNewStack(wrapper, taskCount));
        }

        /// <summary>
        /// DiVoid #7749: the CF-1 residual, in QA's own reproduction shape — a lambda captured <em>outside</em>
        /// the concurrently-invoked bodies, invoked from <em>inside</em> each of several. Before the residual
        /// fix this measured as a genuine breach (4 bodies × depth 3 = 12 &gt; MaxDepth 8) even though no
        /// single body individually approaches the ceiling — concurrency counted as nesting, surviving the
        /// CF-1 fix precisely because that fix only resets the budget for the lambda passed directly to
        /// <c>task.run</c>, not one merely invoked from within it. The <see cref="SyncGate"/> forces all four
        /// bodies to be simultaneously at their deepest recursion level before any of them unwinds, so a pass
        /// is evidence the shared-vs-task-local distinction is resolved correctly, not that the bodies
        /// happened to run sequentially
        /// </summary>
        [Test, Parallelizable, MaxTime(5000)]
        public void Depth_ConcurrentTasksThroughOuterCapturedLambdaDoNotSpuriouslyBreach() {
            const int taskCount = 4;
            const int recursionDepth = 3;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  $gate.Arrive()",
                "  return(0)",
                "}",
                "$wrapper = []=>{ $fac.invoke(" + recursionDepth + ") }",
                "$wrapper"
            ));

            SyncGate gate = new(taskCount);
            VariableProvider variables = new(new Variable("gate", gate));
            LambdaMethod wrapper = (LambdaMethod) definitions.Execute(variables);

            Assert.DoesNotThrow(() => RunConcurrentInvokeOnNewStack(wrapper, taskCount));
        }

        /// <summary>
        /// DiVoid #7744 CF-5: the same <c>N &gt; MaxDepth</c> concurrency-counted-as-nesting shape as
        /// <see cref="Depth_ConcurrentTaskRunLambdasDoNotSpuriouslyBreach"/>, but through
        /// <c>EnumerableExtensions.Where</c> rather than <c>$lambda.invoke()</c> — CF-1's original fix covered
        /// only the script-level <c>.invoke()</c> dispatch and missed this one, which is the overload
        /// <c>.where</c>/<c>.indexof(predicate)</c>/<c>.lastindexof(predicate)</c> actually use. A predicate
        /// captured once, shared across <c>N</c> concurrently-invoked bodies (no recursion anywhere), must not
        /// breach a ceiling none of them individually approaches
        /// </summary>
        [Test, Parallelizable, MaxTime(5000)]
        public void Depth_ConcurrentWherePredicateDoesNotSpuriouslyBreach() {
            const int taskCount = SafeMaxDepth + 1;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$pred = $x=>{",
                "  $gate.Arrive()",
                "  return(true)",
                "}",
                "$wrapper = []=>{ $src.where($pred).toarray() }",
                "$wrapper"
            ));

            SyncGate gate = new(taskCount);
            VariableProvider variables = new(
                new Variable("gate", gate),
                new Variable("src", new object[] {1}));
            LambdaMethod wrapper = (LambdaMethod) definitions.Execute(variables);

            Assert.DoesNotThrow(() => RunConcurrentInvokeOnNewStack(wrapper, taskCount));
        }

        /// <summary>
        /// the task.run sibling of <see cref="Try_DoesNotSwallowDepthAbort"/> (DiVoid #7744 CF-2): a depth
        /// breach from real recursion <em>inside</em> a task.run body must still reach the host through a
        /// script <c>try</c>/<c>catch</c> around <c>task.waitall(...)</c>. <c>Task.WaitAll</c> wraps the
        /// faulted task's exception in <see cref="AggregateException"/> before reflection wraps that in turn
        /// in <see cref="System.Reflection.TargetInvocationException"/> — two layers instead of the one
        /// <c>MethodOperations.cs:292</c>'s filter originally unwrapped, which is why this specific shape
        /// slipped through before the fix even though <see cref="Try_DoesNotSwallowDepthAbort"/> already
        /// passed. Deliberately recurses inside the task body rather than reusing the CF-1 shape above: after
        /// the CF-1 fix each task gets its own fresh, zeroed <see cref="DepthBudget"/>, so N non-recursive
        /// bodies no longer breach anything at all — a real breach requires genuine recursion within one body
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Try_DoesNotSwallowDepthAbortThroughTaskRun() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$t = task.run([]=>{",
                "  $fac = $n=>{",
                "    if($n>0) {",
                "      return($fac.invoke($n-1))",
                "    }",
                "    return(0)",
                "  }",
                "  $fac.invoke(1000000)",
                "})",
                "try {",
                "  task.waitall([$t])",
                "} catch {",
                "  $flag.Caught = true",
                "}"
            ));

            MutableFlag flag = new();
            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute(new VariableProvider(
                new Variable("task", new TaskHost()),
                new Variable("flag", flag))));
            Assert.That(flag.Caught, Is.False);
        }

        /// <summary>
        /// DiVoid #7749: same shape as <see cref="Try_DoesNotSwallowDepthAbortThroughTaskRun"/> — a lambda
        /// captured <em>outside</em> the <c>task.run</c> body, invoked recursively from <em>inside</em> it,
        /// wrapped in a script <c>try</c>/<c>catch</c> around <c>task.waitall(...)</c> — kept as its own test
        /// because of what it proved during development, not because it currently behaves differently from
        /// the sibling test. Sequenced deliberately (DiVoid #7744/#7749): written and confirmed load-bearing
        /// <em>before</em> the CF-1 residual fix existed, when this exact shape was the one scenario where
        /// <see cref="DepthBudget.CheckBreached"/>'s latch alone (aggregate-unwrap fix disabled) still
        /// correctly propagated — because the breach then landed on the shared root budget the outer thread's
        /// later <see cref="ScriptContext.Guard"/> calls also consult. Re-verified after the residual fix
        /// landed: the latch-alone case now fails here too, exactly like the task-local sibling — the residual
        /// fix resolves this lambda's budget from the invoking (task-local) context, not its captured one, so
        /// the breach no longer lands on a shared instance at all. See <see cref="DepthBudget.CheckBreached"/>'s
        /// own remarks for the up-to-date statement of what the latch actually covers now. This test still
        /// earns its place as a permanent regression test for the aggregate-unwrap fix on this specific,
        /// idiomatic shape (a shared helper lambda invoked from a task), independent of the latch question.
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Try_DoesNotSwallowDepthAbortThroughOuterCapturedTaskLambda() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$t = task.run([]=>{ $fac.invoke(1000000) })",
                "try {",
                "  task.waitall([$t])",
                "} catch {",
                "  $flag.Caught = true",
                "}"
            ));

            MutableFlag flag = new();
            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute(new VariableProvider(
                new Variable("task", new TaskHost()),
                new Variable("flag", flag))));
            Assert.That(flag.Caught, Is.False);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_SequentialLambdaCallbacksDoNotAccumulateDepth() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = 4}
            };
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.where($x=>$x>0).count()");

            int[] source = Enumerable.Range(-5000, 10000).ToArray();
            int result = script.Execute<int>(new VariableProvider(new Variable("src", source)));
            Assert.AreEqual(source.Count(x => x > 0), result);
        }

        /// <summary>
        /// CF-1-shaped: <c>AssignableToken.Assign</c> executes the right-hand side inline, so a depth breach
        /// raised while evaluating it must reach the caller as <see cref="ScriptDepthLimitExceededException"/>,
        /// not a wrapped <see cref="ScriptRuntimeException"/>
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_BreachDuringAssignmentRhsSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "$x = $fac.invoke(1000000)");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// the round-2 <c>Throw.cs</c> site: <c>throw($fac.invoke(...))</c> evaluates its message expression
        /// inline, so a depth breach there must also reach the caller unwrapped
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_BreachDuringThrowExpressionSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "throw($fac.invoke(1000000))");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// exercises the <c>MethodOperations.cs:292</c> <see cref="System.Reflection.TargetInvocationException"/>
        /// filter with a reflected host method distinct from <c>.invoke()</c> itself
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_BreachInsideReflectedHostCallbackSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<ExecutionGuardTests>();
            IScript script = ParseRecursiveFactorial(parser, "$fac.invokecallback(1000000)");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// exercises <c>ScriptMethod</c>'s <see cref="IExternalMethod"/> catch chain (the <c>import(...).invoke()</c>
        /// shape), distinct from <see cref="Depth_MutuallyRecursiveImportChainThrows"/>'s two-parser crossing
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_BreachInsideSingleImportInvokeSurfacesUnwrapped() {
            ScriptParser innerParser = new();
            IScript imported = ParseRecursiveFactorial(innerParser, "$fac.invoke(1000000)");

            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth},
                ImportProvider = new FixedImportProvider(new ExternalScriptMethod(imported))
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$m = import(\"whatever\")",
                "$m.invoke()"
            ));

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// host object whose flag a catch body can flip; used instead of a script variable because an
        /// assignment inside a nested scope (a lambda body, an <c>if</c>, a <c>catch</c>) auto-declares in
        /// that scope rather than writing through to an ancestor unless the name was already declared there
        /// (<c>Tokens/ScriptVariable.cs</c>'s <c>AssignToken</c>) - a host-held mutation is the only way to
        /// observe "did the catch body run" from the test that isn't itself sensitive to scoping
        /// </summary>
        class MutableFlag {
            public bool Caught { get; set; }
        }

        /// <summary>
        /// the highest-value test in this file: a script-level <c>try</c>/<c>catch</c> around a depth breach
        /// must not swallow it. Asserts both that the exception propagates and that the <c>catch</c> body
        /// never ran (the host-held flag was never flipped)
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Try_DoesNotSwallowDepthAbort() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "try {",
                "  $fac.invoke(1000000)",
                "} catch {",
                "  $flag.Caught = true",
                "}"
            ));

            MutableFlag flag = new();
            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute(new VariableProvider(new Variable("flag", flag))));
            Assert.That(flag.Caught, Is.False);
        }

        /// <summary>
        /// concrete reachability proof for DiVoid #7734: an imported script whose own parser configures
        /// <see cref="ScriptLimits.Timeout"/> raises <see cref="ScriptTimeoutException"/> from inside the
        /// outer script's token tree (not at the unreachable outermost <c>Script.Execute</c> boundary), so an
        /// outer <c>try</c>/<c>catch</c> around the call must not swallow it. Before this PR, <c>Try.cs</c>
        /// rethrew only <see cref="ScriptStepLimitExceededException"/> by name and this scenario fell through
        /// to the generic catch; catching the <see cref="ScriptAbortException"/> base closes the gap
        /// structurally, without naming <see cref="ScriptTimeoutException"/> at this site at all
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Try7734_ImportedScriptTimeoutIsNotSwallowedByOuterCatch() {
            ScriptParser innerParser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(100)}
            };
            IScript imported = innerParser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));

            ScriptParser parser = new() {
                ImportProvider = new FixedImportProvider(new ExternalScriptMethod(imported))
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$m = import(\"whatever\")",
                "try {",
                "  $m.invoke()",
                "} catch {",
                "  $flag.Caught = true",
                "}"
            ));

            MutableFlag flag = new();
            Assert.Throws<ScriptTimeoutException>(() => script.Execute(new VariableProvider(new Variable("flag", flag))));
            Assert.That(flag.Caught, Is.False);
        }

        /// <summary>
        /// DiVoid #7744 CF-4: the <see cref="ScriptMethod"/> <see cref="IExternalMethod"/> fast path had no
        /// <see cref="ScriptRuntimeException"/> clause, so any ordinary script error inside a
        /// script-invoked lambda's body fell through to the generic <c>catch(Exception)</c> and had its
        /// specific message discarded into a boilerplate "Error calling external method" wrapper — the actual
        /// cause survived only as an inner exception, which host logging keyed on <c>.Message</c> never sees.
        /// This hits every error in every script-invoked lambda since DiVoid #7749 routed
        /// <c>$lambda.invoke()</c> through this path. Mirrors the fix already in place for the resolved-method
        /// chain (<c>ScriptMethod.cs</c>'s other <c>catch(ScriptRuntimeException e)</c> clause)
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Invoke_PreservesInnerErrorMessage() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$f = $a=>{ return($a.nosuchmethod()) }",
                "$f.invoke(1)"
            ));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.Message, Does.Contain("nosuchmethod"));
        }

        /// <summary>
        /// compile-only regression guard for DiVoid #7744 round 4/5 — deliberately never called, and must
        /// not be "cleaned up" as an apparently-unused private method. An earlier version of the CF-5 fix
        /// added a second public <c>Invoke</c> overload (<c>Invoke(ScriptContext, params object[])</c>)
        /// alongside the existing <c>Invoke(params object[])</c>. QA test-compiled the fallout rather than
        /// reasoning about it and found the <em>bare <c>null</c> literal</em> specifically — not a
        /// statically-<c>object</c>-typed argument, which round 4's first version of this guard used and
        /// which QA found was <em>never</em> ambiguous under either shape — stopped compiling at all
        /// (<c>CS0121</c>, ambiguous between the two overloads). That is why the line below is exactly
        /// <c>lambda.Invoke(null)</c>, not <c>lambda.Invoke((object) null)</c>: only the former exercises the
        /// ambiguity CS0121 reports on.
        /// <para>
        /// This method is never invoked because calling it would throw — a bare <c>null</c> literal against
        /// today's single <c>Invoke(params object[])</c> overload binds in <em>normal</em> form (the whole
        /// array reference is <c>null</c>), which <c>LambdaMethod.CheckArguments</c> now handles explicitly
        /// (see <see cref="Invoke_NullArgumentArrayIsTreatedAsZeroArguments"/> for the runtime half of this pair).
        /// Its only job is to exist as compiled code: if a second <c>Invoke</c> overload is ever
        /// reintroduced, this line stops compiling and breaks the whole project's build — the only way CS0121
        /// can be observed at all, since it is a compile-time diagnostic with no runtime trace to assert on.
        /// </para>
        /// <para>
        /// Falsified before being trusted: temporarily reintroduced
        /// <c>public object Invoke(ScriptContext, params object[])</c> alongside the existing overload and
        /// confirmed the build broke here with <c>CS0121</c>, at this exact line, before restoring the
        /// single-overload shape and confirming the build was clean again.
        /// </para>
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        static void CompileOnly_InvokeBareNullLiteralResolvesToSingleOverload(LambdaMethod lambda) {
            lambda.Invoke(null);
        }

        /// <summary>
        /// the runtime half of the pair with <see cref="CompileOnly_InvokeBareNullLiteralResolvesToSingleOverload"/>
        /// (DiVoid #7744 round 5): a bare <c>null</c> literal binds to <c>Invoke</c>'s <c>params object[]</c>
        /// parameter in normal form, so <c>arguments</c> itself is <c>null</c> rather than a one-element array
        /// — <c>LambdaMethod.CheckArguments</c> must treat that as zero arguments rather than dereferencing
        /// <c>arguments.Length</c> directly, or a zero-parameter lambda invoked this way throws
        /// <see cref="NullReferenceException"/> instead of succeeding
        /// </summary>
        [Test]
        public void Invoke_NullArgumentArrayIsTreatedAsZeroArguments() {
            ScriptParser parser = new();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$const = []=>{ return(42) }",
                "$const"
            ));
            LambdaMethod constLambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            object result = constLambda.Invoke(null);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task Depth_AsyncPathTaskIsFaultedNotCanceled() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "$fac.invoke(1000000)");

            Task task = script.ExecuteAsync((IVariableProvider)null, CancellationToken.None);
            await task.ContinueWith(t => { });

            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.IsCanceled, Is.False);
            Assert.That(task.Exception?.InnerException, Is.InstanceOf<ScriptDepthLimitExceededException>());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Depth_SyncTokenPathThrowsDepthException() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "$fac.invoke(1000000)");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute((IVariableProvider)null, CancellationToken.None));
        }

        /// <summary>
        /// a configured <see cref="ScriptLimits.MaxDepth"/> must not disturb the existing cancel/timeout
        /// contract for a script that never triggers the depth guard
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public async Task Depth_CallerCancelDuringDepthLimitedScriptStillCanceled() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = 1000000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        /// <summary>
        /// extends <c>CancellationSupportTests.CF2_DefaultLimitsAreNotSharedMutableState</c> to
        /// <see cref="ScriptLimits.MaxDepth"/>: a configured parser's depth ceiling must not leak onto a
        /// separately constructed, default parser sharing <see cref="ScriptLimits.None"/>. A recursion one
        /// level deeper than the configured (but unrelated) parser's ceiling would breach it if leaked, so a
        /// pass is evidence of isolation, not merely of a short script
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void CF2Extended_MaxDepthDefaultsNullAndDoesNotLeakAcrossParsers() {
            ScriptParser configuredParser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth - 3}
            };
            ScriptParser defaultParser = new();

            Assert.That(defaultParser.Limits, Is.SameAs(ScriptLimits.None));
            Assert.That(defaultParser.Limits.MaxDepth, Is.Null);
            Assert.That(configuredParser.Limits.MaxDepth, Is.EqualTo(SafeMaxDepth - 3));

            IScript script = ParseRecursiveFactorial(defaultParser, "$fac.invoke(" + SafeMaxDepth + ")");
            Assert.DoesNotThrow(() => script.Execute());
        }

        /// <summary>
        /// T31b: a default execution (no <see cref="ScriptLimits.MaxDepth"/> configured) must not allocate a
        /// <see cref="DepthBudget"/> at all (design §12 S2) — previously verifiable only by reading
        /// <c>GuardedExecution.cs:56</c>, now asserted directly against the internal fields it sets
        /// </summary>
        [Test]
        public void T31b_DefaultExecutionAllocatesNoDepthBudget() {
            using GuardedExecution execution = GuardedExecution.Prepare(new VariableProvider(), null, CancellationToken.None, ScriptLimits.None);

            Assert.That(execution.Context.DepthBudget, Is.Null);
            Assert.That(execution.Context.StepBudget, Is.Null);
        }

        /// <summary>
        /// direct unit test for <see cref="DepthBudget.CheckBreached"/> (DiVoid #7744, QA round 3): a claimed
        /// guarantee must be verifiable, not merely inferred from the shape of other tests that happen to
        /// exercise it indirectly — "untested code gets deleted by the next person who greps for callers."
        /// Breaches a budget directly via <see cref="DepthBudget.Enter"/> (<c>Limit = 0</c>, so the first entry
        /// already exceeds it), then asserts <see cref="DepthBudget.CheckBreached"/> also throws afterwards —
        /// the latch behaviour itself, independent of any script-level swallow scenario
        /// </summary>
        [Test]
        public void DepthBudget_CheckBreachedThrowsAfterBreach() {
            DepthBudget budget = new(0);

            Assert.Throws<ScriptDepthLimitExceededException>(() => budget.Enter());
            Assert.Throws<ScriptDepthLimitExceededException>(() => budget.CheckBreached());
        }
    }
}

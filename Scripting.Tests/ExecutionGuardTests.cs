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
        /// <c>Operations/MethodOperations.cs:292</c>
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
        /// safe recursion ceiling for tests in this file, measured against the reflected-invoke dispatch path
        /// (see docs/architecture/execution-guards-depth-memory.md §11.1/§11.2); raising it needs re-measuring
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

        [Test, Parallelizable, MaxTime(5000)]
        [Description("Pins the DepthBudget enter/exit pairing across 1000 throw-unwind repetitions; a leak here would make a later, entirely legal call spuriously breach.")]
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
        /// drives <paramref name="count"/> invocations of <see cref="LambdaMethod.InvokeOnNewStack"/> from
        /// that many dedicated, test-owned <see cref="Thread"/> instances rather than the .NET thread pool, so
        /// concurrency does not depend on pool availability; still exercises the exact production method
        /// <see cref="Hosts.TaskHost.Run"/> calls
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

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7744 CF-1: N > MaxDepth concurrent, non-recursive lambda invocations on separate physical stacks must not breach a ceiling none of them individually approaches; sized at the exact boundary QA measured, with a SyncGate forcing true simultaneity.")]
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

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7749 CF-1 residual: a lambda captured outside several concurrently-invoked task bodies, invoked from inside each, must not have concurrency counted as nesting against a shared budget.")]
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

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7744 CF-5: the same concurrency-counted-as-nesting shape as Depth_ConcurrentTaskRunLambdasDoNotSpuriouslyBreach, but through EnumerableExtensions.Where/IndexOf/LastIndexOf rather than $lambda.invoke(), which CF-1's original fix missed.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7744 CF-2: a depth breach from real recursion inside a task.run body must reach the host through a script try/catch around task.waitall(), even though Task.WaitAll wraps it two AggregateException/TargetInvocationException layers deep.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7749: same shape as Try_DoesNotSwallowDepthAbortThroughTaskRun, but with the recursive lambda captured outside the task body; kept as a permanent regression test for the aggregate-unwrap fix on this shared-helper-lambda shape.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("CF-1-shaped: AssignableToken.Assign executes its right-hand side inline, so a depth breach there must reach the caller unwrapped, not wrapped in ScriptRuntimeException.")]
        public void Depth_BreachDuringAssignmentRhsSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "$x = $fac.invoke(1000000)");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("The round-2 Throw.cs site: throw($fac.invoke(...)) evaluates its message expression inline, so a depth breach there must also reach the caller unwrapped.")]
        public void Depth_BreachDuringThrowExpressionSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript script = ParseRecursiveFactorial(parser, "throw($fac.invoke(1000000))");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Exercises the TargetInvocationException passthrough filter with a reflected host method distinct from .invoke() itself.")]
        public void Depth_BreachInsideReflectedHostCallbackSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<ExecutionGuardTests>();
            IScript script = ParseRecursiveFactorial(parser, "$fac.invokecallback(1000000)");

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Exercises ScriptMethod's IExternalMethod catch chain (the import(...).invoke() shape), distinct from Depth_MutuallyRecursiveImportChainThrows's two-parser crossing.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("A script-level try/catch around a depth breach must not swallow it — asserts both that the exception propagates and that the catch body never ran.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7734: an imported script's own ScriptTimeoutException, raised from inside the outer script's token tree, must not be swallowed by an outer try/catch that only named ScriptStepLimitExceededException.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7744 CF-4: the ScriptMethod IExternalMethod fast path must not discard a script-invoked lambda's own error message into a generic wrapper — mirrors the existing fix for the resolved-method chain.")]
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
        /// compile-only regression guard: never called, and must not be "cleaned up" as an apparently-unused
        /// private method. If a second <c>Invoke</c> overload is ever reintroduced, the line below stops
        /// compiling (<c>CS0121</c>, ambiguous between the two overloads) and breaks the build — the only way
        /// to observe that regression, since it is a compile-time diagnostic with no runtime trace to assert
        /// on. Must stay exactly <c>lambda.Invoke(null)</c>, not <c>lambda.Invoke((object) null)</c>: only the
        /// bare literal exercises the ambiguity
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        static void CompileOnly_InvokeBareNullLiteralResolvesToSingleOverload(LambdaMethod lambda) {
            lambda.Invoke(null);
        }

        [Test]
        [Description("The runtime half of the pair with CompileOnly_InvokeBareNullLiteralResolvesToSingleOverload (DiVoid #7744 round 5): a bare null literal must be treated as zero arguments, not dereferenced.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("A configured MaxDepth must not disturb the existing cancel/timeout contract for a script that never triggers the depth guard.")]
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

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Extends CancellationSupportTests.CF2_DefaultLimitsAreNotSharedMutableState to MaxDepth: a configured parser's depth ceiling must not leak onto a separately constructed default parser.")]
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

        [Test]
        [Description("T31b (design §12 S2): a default execution with no MaxDepth configured must not allocate a DepthBudget at all.")]
        public void T31b_DefaultExecutionAllocatesNoDepthBudget() {
            using GuardedExecution execution = GuardedExecution.Prepare(new VariableProvider(), null, CancellationToken.None, ScriptLimits.None);

            Assert.That(execution.Context.DepthBudget, Is.Null);
            Assert.That(execution.Context.StepBudget, Is.Null);
        }

        [Test]
        [Description("Direct unit test for DepthBudget.CheckBreached (DiVoid #7744 QA round 3): a claimed guarantee must be independently verifiable, not merely inferred from other tests that happen to exercise it indirectly.")]
        public void DepthBudget_CheckBreachedThrowsAfterBreach() {
            DepthBudget budget = new(0);

            Assert.Throws<ScriptDepthLimitExceededException>(() => budget.Enter());
            Assert.Throws<ScriptDepthLimitExceededException>(() => budget.CheckBreached());
        }
    }
}

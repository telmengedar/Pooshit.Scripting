using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Pooshit.Scripting.Parser.Resolvers;
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

        /// <summary>
        /// test-local host extension exercising both public <see cref="LambdaMethod"/> invocation entry
        /// points against the same lambda, so the split between them (design §7.7) can be pinned without
        /// <see cref="EnumerableExtensions"/> masking it - converting an in-repo caller could otherwise make
        /// a regression here pass unnoticed
        /// </summary>
        class InvokeSplitExtensions {

            /// <summary>
            /// invokes <paramref name="callback"/> through the invoking context rather than the context it
            /// was captured in
            /// </summary>
            /// <param name="callback">lambda to invoke</param>
            /// <param name="argument">argument passed to the lambda</param>
            /// <param name="context">invoking context; injected by the engine, not by the script call</param>
            /// <returns>result of the callback invocation</returns>
            public static object InvokeFromCaller(LambdaMethod callback, object argument, ScriptContext context) => callback.InvokeFrom(context, argument);

            /// <summary>
            /// invokes <paramref name="callback"/> through its own captured context
            /// </summary>
            /// <param name="callback">lambda to invoke</param>
            /// <param name="argument">argument passed to the lambda</param>
            /// <returns>result of the callback invocation</returns>
            public static object InvokeFromCapture(LambdaMethod callback, object argument) => callback.Invoke(argument);

            /// <summary>
            /// invokes <paramref name="callback"/> as a host dispatch opening a fresh execution scope
            /// </summary>
            /// <param name="callback">lambda to invoke</param>
            /// <param name="argument">argument passed to the lambda</param>
            /// <returns>result of the callback invocation</returns>
            public static object InvokeAsNewExecution(LambdaMethod callback, object argument) => callback.InvokeAsExecution(argument);
        }

        /// <summary>
        /// host type exposing an <see cref="IFormattable"/> property, exercising H1 on a host-injected type
        /// (design §7.6.2, T76)
        /// </summary>
        class FormattableHost {
            public DateTime When { get; set; } = new(2024, 1, 1);
        }

        /// <summary>
        /// host type whose member's own allocation is commanded by an integral argument, exercising H2
        /// (design §7.6.3, T77)
        /// </summary>
        class RepeatingHost {
            public string Repeat(int n) => new('x', n);
        }

        /// <summary>
        /// host type whose member allocates at a per-unit cost H2's one-byte guess cannot see, exercising a
        /// host <see cref="MethodGuard.Charge{T}"/> row (design §7.6.3/§10.2, T79)
        /// </summary>
        class BufferHost {
            public byte[] MakeBuffers(int count) => new byte[count];
        }

        /// <summary>
        /// host type with a zero-argument member no shape rule can bound, exercising a host
        /// <see cref="MethodGuard.Deny{T}"/> entry (design §7.6.5, T80/T81)
        /// </summary>
        class RenderHost {
            public string Render() => new('x', 1);
        }

        /// <summary>
        /// ordinary host domain type with ten zero-arg getters, a setter, a filter callback and an id lookup -
        /// the usability probe H3 permit-by-shape must pass with zero host configuration (design §7.6.4, T78)
        /// </summary>
        class OrdinaryDomainHost {
            public string GetName() => "x";
            public int GetAge() => 1;
            public bool GetActive() => true;
            public double GetScore() => 1.0;
            public string GetDescription() => "d";
            public string GetCategory() => "c";
            public int GetRank() => 1;
            public bool GetVerified() => true;
            public double GetWeight() => 1.0;
            public string GetTag() => "t";
            public void SetName(string name) { }
            public bool Find(OrdinaryDomainHost filter) => filter != null;
            public OrdinaryDomainHost GetById(int id) => this;
        }

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

        /// <summary>
        /// generous ceiling for the M4 allocation-delta proof tests (T41/T43/T44/T45, design §8.8.6) - the
        /// requested capacities in those tests would allocate hundreds of MB if the pre-allocation charge
        /// fired after the underlying allocation instead of before it, so this only needs enough headroom to
        /// absorb ordinary test-process allocation noise, not to approach that spike
        /// </summary>
        const long MaxPreAllocationGuardDeltaBytes = 50_000_000;

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
        /// synchronisation gate that releases every participant only once all of them have arrived, used to
        /// make N concurrent invocations provably simultaneously "in flight" rather than depending on
        /// scheduling luck. <paramref name="onEveryoneArrived"/>, when given, runs exactly once per phase - on
        /// whichever participant thread happens to arrive last, strictly after every participant has signalled
        /// and strictly before any of them is released to continue (guaranteed by <see cref="Barrier"/>'s own
        /// post-phase-action contract). That is the one synchronisation point this type can offer
        /// deterministically without depending on real-time scheduling, and
        /// <see cref="Depth_HostExtensionInvokeArgsSpuriouslyBreaches"/> relies on it rather than on a
        /// second, independently-timed gate. The wait itself is bounded only as a deadlock backstop: it must
        /// fail loudly rather than let the barrier release early with partial participants
        /// </summary>
        class SyncGate {
            static readonly TimeSpan ArrivalTimeout = TimeSpan.FromSeconds(30);

            readonly Barrier barrier;
            readonly int participants;

            public SyncGate(int participants, Action onEveryoneArrived = null) {
                this.participants = participants;
                barrier = onEveryoneArrived == null
                    ? new Barrier(participants)
                    : new Barrier(participants, _ => onEveryoneArrived());
            }

            public object Arrive() {
                if (!barrier.SignalAndWait(ArrivalTimeout))
                    throw new TimeoutException($"SyncGate timed out after {ArrivalTimeout} waiting for all {participants} participants to arrive - either a genuine deadlock or the timeout is too short for current scheduler load");
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

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7782 T9a: a test-local host extension declaring a trailing ScriptContext and calling InvokeFrom must not accumulate concurrency as depth - pins §11.6's prescribed host-extension pattern against the exact shape QA #7744 round 3 measured. Deliberately not EnumerableExtensions, so converting an in-repo caller can never make this pass without fixing the extension under test.")]
        public void Depth_HostExtensionInvokeFromDoesNotSpuriouslyBreach() {
            const int taskCount = SafeMaxDepth + 1;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<InvokeSplitExtensions>();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$pred = $x=>{",
                "  $gate.Arrive()",
                "  return(true)",
                "}",
                "$wrapper = []=>{ $pred.invokefromcaller(1) }",
                "$wrapper"
            ));

            SyncGate gate = new(taskCount);
            VariableProvider variables = new(new Variable("gate", gate));
            LambdaMethod wrapper = (LambdaMethod) definitions.Execute(variables);

            Assert.DoesNotThrow(() => RunConcurrentInvokeOnNewStack(wrapper, taskCount));
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7782 T9b - characterisation test: the same test-local extension's second method, calling Invoke(args) instead of InvokeFrom, charges every concurrent callback to the defining script's single counter. Pins §7.7's decision that Invoke's captured-context semantics are deliberate; a failure here means the contract changed and that must be a re-argued decision, not a silent one. Sequenced by a Barrier post-phase action, not raced against a second, independently-timed gate: this test starts exactly SafeMaxDepth threads racing for the shared budget - none can breach yet, since only SafeMaxDepth attempts exist - and lets SyncGate's post-phase action, which Barrier guarantees runs only once every one of them is confirmed blocked at the gate, start a 9th, non-participant thread on the spot. That 9th Enter() is thus guaranteed to observe depth SafeMaxDepth+1 and breach directly. DepthBudget.CheckBreached's sticky latch (DepthBudget.cs) then re-raises that same breach for every one of the SafeMaxDepth holders too, deterministically, the moment each is released and reaches its own next Guard() checkpoint (ScriptContext.Guard(), called by StatementBlock before every statement - here, the 'return(true)' following $gate.Arrive()): the Interlocked.Exchange that trips the latch happens-before the Barrier release that lets them proceed, so by the time any of them re-checks, the latch is already visible. So all SafeMaxDepth+1 invocations end up observing ScriptDepthLimitExceededException, which is itself the sticky-latch half of this same characterisation and worth pinning alongside the direct breach.")]
        public void Depth_HostExtensionInvokeArgsSpuriouslyBreaches() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<InvokeSplitExtensions>();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$pred = $x=>{",
                "  $gate.Arrive()",
                "  return(true)",
                "}",
                "$wrapper = []=>{ $pred.invokefromcapture(1) }",
                "$wrapper"
            ));

            LambdaMethod wrapper = null;
            Exception breachException = null;
            SyncGate gate = new(SafeMaxDepth, onEveryoneArrived: () => {
                Thread breachingThread = new(() => {
                    try {
                        wrapper.InvokeOnNewStack();
                    }
                    catch (Exception e) {
                        breachException = e;
                    }
                });
                breachingThread.Start();
                breachingThread.Join();
            });

            VariableProvider variables = new(new Variable("gate", gate));
            wrapper = (LambdaMethod) definitions.Execute(variables);

            Exception[] holdingExceptions = new Exception[SafeMaxDepth];
            Thread[] holdingThreads = new Thread[SafeMaxDepth];
            for (int i = 0; i < SafeMaxDepth; i++) {
                int index = i;
                holdingThreads[i] = new Thread(() => {
                    try {
                        wrapper.InvokeOnNewStack();
                    }
                    catch (Exception e) {
                        holdingExceptions[index] = e;
                    }
                });
            }

            foreach (Thread thread in holdingThreads)
                thread.Start();
            foreach (Thread thread in holdingThreads)
                thread.Join();

            Assert.IsInstanceOf<ScriptDepthLimitExceededException>(breachException);
            Assert.That(holdingExceptions, Is.All.InstanceOf<ScriptDepthLimitExceededException>());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7782 T9c - the load-bearing test: recursion routed through the test-local Invoke(args) extension at every level must still throw ScriptDepthLimitExceededException. This is what fails if Invoke is ever given a fresh budget per call (§7.7 option d), which would otherwise read depth 1 forever while the physical stack grows unbounded. The recursion literal is finite and well above MaxDepth but well below the measured crash depth - an unbounded literal would kill the test runner instead of failing the assertion under that regression.")]
        public void Depth_RecursionThroughInvokeArgsExtensionAtEveryLevelThrows() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<InvokeSplitExtensions>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invokefromcapture($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac.invokefromcapture(32)"
            ));

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
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
        [Description("DiVoid #7886 T3: a host extension declaring a trailing ScriptContext and calling InvokeFrom must resolve the WHOLE governing budget set from the invoking context, not just depth — the lambda is defined under a tight MaxSteps=100 parser but dispatched 500 times under a MaxSteps=1_000_000 invoker. Fails today (pre-fix) with ScriptStepLimitExceededException at the definer's limit, matching diagnosis #7888 E12.")]
        public void Step_InvokeFromResolvesWholeBudgetFromInvokingContext() {
            ScriptParser definerParser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
            };
            IScript definitions = definerParser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            ScriptParser invokerParser = new() {
                Limits = new ScriptLimits {MaxSteps = 1_000_000}
            };
            invokerParser.Extensions.AddExtensions<InvokeSplitExtensions>();
            IScript invoker = invokerParser.Parse(ScriptCode.Create(
                "for($i=0,$i<500,++$i) {",
                "  $lambda.invokefromcaller(1)",
                "}"
            ));

            Assert.DoesNotThrow(() => invoker.Execute(new VariableProvider(new Variable("lambda", lambda))));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7886 T4b: generalising InvokeFrom must not open the sandbox hole either — a script looping through a host extension that calls InvokeFrom on a cached lambda defined in the SAME execution must still be bounded by that execution's own step budget, since the invoking context is the running script's own. Would time out instead of throwing if InvokeFrom were ever given a fresh budget per call.")]
        public void Step_LoopThroughInvokeFromDoesNotEscapeStepBudget() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
            };
            parser.Extensions.AddExtensions<InvokeSplitExtensions>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "while(true) {",
                "  $f.invokefromcaller(1)",
                "}"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7886: EnumerableExtensions.Where already calls InvokeFrom for every in-repo predicate dispatch (definer == invoker there), so the generalised budget resolution must not regress the existing extension - unchanged filtering behaviour under a configured MaxSteps pins the blast-radius claim that in-repo call sites are unaffected.")]
        public void Step_EnumerableWherePredicateUnaffectedByInvokeFromGeneralisation() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 10000}
            };
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.where($x=>$x>0).count()");

            int[] source = Enumerable.Range(-50, 100).ToArray();
            int result = script.Execute<int>(new VariableProvider(new Variable("src", source)));
            Assert.AreEqual(source.Count(x => x > 0), result);
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7880 T1: parse once, cache the lambda, dispatch InvokeAsExecution 10,000 times under MaxSteps=1000 with a small fixed-cost body — no throw. Fails today (before this fix) at ~invoke #77 (diagnosis #7888 E1) because Invoke's captured StepBudget is a lifetime counter; InvokeAsExecution must open a fresh one per dispatch.")]
        public void Step_CachedLambdaInvokeAsExecutionDoesNotAccumulateAcrossDispatches() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 1000}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            Assert.DoesNotThrow(() => {
                for (int i = 0; i < 10000; i++)
                    lambda.InvokeAsExecution(1);
            });
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7885 T2: Timeout=100ms, body sleeps ~300ms via the interruptible wait(), three separate InvokeAsExecution dispatches — ScriptTimeoutException on EVERY dispatch including the first. 'Including the first' is load-bearing: fails today because a cached lambda's captured token belongs to a CancellationTokenSource GuardedExecution.Dispose already tore down un-fired (diagnosis #7888 E4b/E16 measured three ~900ms invokes all completing under a 300ms Timeout); it would also fail under a fix that merely resets rather than re-arms the deadline.")]
        public void Timeout_CachedLambdaInvokeAsExecutionThrowsOnEveryDispatchIncludingFirst() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(100)}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = []=>{ wait(300) return(0) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            for (int i = 0; i < 3; i++)
                Assert.Throws<ScriptTimeoutException>(() => lambda.InvokeAsExecution());
        }

        [Test, MaxTime(5000)]
        [Description("DiVoid #7897: ScriptLimits.Timeout must still fire under a starved thread pool instead of depending on CancelAfter's pool-scheduled callback landing.")]
        public void Timeout_HoldsUnderStarvedThreadPool() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(100)}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "wait(300)",
                "return(0)"
            ));

            ThreadPool.GetMinThreads(out int minWorker, out int minIo);
            ThreadPool.SetMinThreads(1, minIo);
            ManualResetEventSlim release = new(false);
            int hogCount = Environment.ProcessorCount * 4;
            CountdownEvent hogsDone = new(hogCount);
            try {
                for (int h = 0; h < hogCount; h++)
                    ThreadPool.UnsafeQueueUserWorkItem(_ => {
                        release.Wait(500);
                        hogsDone.Signal();
                    }, null);

                ScriptTimeoutException exception = null;
                long elapsedMs = -1;
                Thread worker = new(() => {
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    try {
                        script.Execute();
                    }
                    catch (ScriptTimeoutException e) {
                        exception = e;
                    }
                    elapsedMs = stopwatch.ElapsedMilliseconds;
                }) {IsBackground = true};
                worker.Start();
                worker.Join(3000);

                Assert.That(exception, Is.Not.Null);
                Assert.That(elapsedMs, Is.LessThan(1000));
            }
            finally {
                release.Set();
                ThreadPool.SetMinThreads(minWorker, minIo);
                hogsDone.Wait(5000);
                hogsDone.Dispose();
                release.Dispose();
            }
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7897: wait(n) must run its full requested duration when Timeout is configured but nowhere near firing.")]
        public void Wait_DoesNotReturnEarlyWhenDeadlineNotYetReached() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromSeconds(5)}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "wait(200)",
                "return(0)"
            ));

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            object result = script.Execute();
            stopwatch.Stop();

            Assert.That(result, Is.EqualTo(0));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(190));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7897: Invoke() on a cached lambda must not throw after its definition-time deadline has elapsed, because it has no deadline of its own to begin with.")]
        public void Invoke_OnCachedLambdaAfterDefinitionTimeDeadlineElapsedDoesNotThrowObjectDisposed() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(50)}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            Thread.Sleep(200);

            object result = null;
            Assert.DoesNotThrow(() => result = lambda.Invoke(42));
            Assert.That(result, Is.EqualTo(42));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7880 T4 — NEGATIVE, the sandbox guard: a script looping through a host extension that calls Invoke(args) on a cached lambda must NOT get an unlimited step budget merely because InvokeAsExecution exists elsewhere on the surface. Must throw ScriptStepLimitExceededException, never complete — this is the test that fails if any scope-opening behaviour is ever attached to plain Invoke.")]
        public void Step_LoopThroughInvokeArgsExtensionDoesNotEscapeStepBudget() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
            };
            parser.Extensions.AddExtensions<ExecutionGuardTests>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "while(true) {",
                "  $f.invokecallback(1)",
                "}"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7880 T6 — the T5 analogue for the new door: recursion routed through a test-local extension calling InvokeAsExecution at every level must still throw ScriptDepthLimitExceededException, never StackOverflowException. Fails if InvokeAsExecution is ever given a fresh DepthBudget per dispatch instead of inheriting the captured one (§6.3's carve-out) — arguably the single most important new test in this plan. The recursion literal is finite, well above MaxDepth and well below the measured crash depth, for the same reason as T5's.")]
        public void Depth_RecursionThroughInvokeAsExecutionExtensionAtEveryLevelThrows() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            parser.Extensions.AddExtensions<InvokeSplitExtensions>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invokeasnewexecution($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac.invokeasnewexecution(32)"
            ));

            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7880 §6.5 T7: a cached lambda driven to a depth breach via host C# Invoke, then dispatched again via InvokeAsExecution with a shallow argument, must succeed — InvokeAsExecution clears the sticky breach latch at the start of each dispatch, while the physical depth count itself is unaffected (the shallow dispatch would still breach if it weren't). Depth_HostExtensionInvokeArgsSpuriouslyBreaches (unmodified, elsewhere in this file) pins the other half: the latch stays sticky WITHIN one run.")]
        public void Depth_InvokeAsExecutionClearsPriorBreachLatch() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            Assert.Throws<ScriptDepthLimitExceededException>(() => lambda.Invoke(1000000));

            object result = null;
            Assert.DoesNotThrow(() => result = lambda.InvokeAsExecution(1));
            Assert.That(result, Is.EqualTo(0));
        }

        [Test, Parallelizable, MaxTime(3000)]
        [Description("DiVoid #7880 §6.6 T8a: InvokeAsExecution observes the HOST-supplied token — cancelling it mid-dispatch surfaces the original OperationCanceledException, not ScriptTimeoutException (no Timeout is configured on this dispatch, so GuardedExecution.Convert has nothing to translate it into).")]
        public async Task InvokeAsExecution_HostTokenCancelledMidDispatchThrowsOperationCanceled() {
            ScriptParser parser = new();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = []=>{ while(true) { wait(10) } }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            CancellationTokenSource cts = new();
            Task task = Task.Run(() => lambda.InvokeAsExecution(cts.Token));
            cts.CancelAfter(100);

            await task.ContinueWith(t => { });

            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception?.InnerException, Is.InstanceOf<OperationCanceledException>());
            Assert.That(task.Exception?.InnerException, Is.Not.InstanceOf<ScriptTimeoutException>());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7880 §6.6 T8b: a cached lambda's CAPTURED cancellation token being cancelled after the defining Execute() already returned must not affect a later InvokeAsExecution(CancellationToken.None, ...) dispatch — the captured token does not carry over to a detached dispatch, the host supplies its own.")]
        public void InvokeAsExecution_CapturedTokenCancelledAfterDefineDoesNotAffectLaterDispatch() {
            ScriptParser parser = new();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "$f"
            ));

            CancellationTokenSource definerCts = new();
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider(), definerCts.Token);
            definerCts.Cancel();

            object result = null;
            Assert.DoesNotThrow(() => result = lambda.InvokeAsExecution(CancellationToken.None, 42));
            Assert.That(result, Is.EqualTo(42));
        }

        [Test, MaxTime(20000)]
        [Description("DiVoid #7880 T9: N threads x M InvokeAsExecution dispatches on ONE cached lambda, MaxSteps sized so a single dispatch barely fits — reuses diagnosis #7888 E15's exact failing shape (8 threads x 5000 invokes, MaxSteps=100000; measured today: '7/8 threads failed; total successful invokes = 33330') to prove per-dispatch attribution rather than a shared counter. Not [Parallelizable] against other fixtures given its own internal thread load.")]
        public void Step_ConcurrentInvokeAsExecutionOnOneCachedLambdaDoesNotShareStepBudget() {
            const int threadCount = 8;
            const int invokesPerThread = 5000;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 100000}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            Exception[] exceptions = new Exception[threadCount];
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++) {
                int index = i;
                threads[i] = new Thread(() => {
                    try {
                        for (int j = 0; j < invokesPerThread; j++)
                            lambda.InvokeAsExecution(1);
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

            Assert.That(exceptions, Is.All.Null);
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7880 T10 (§6.4): a lambda that mutates a list held in a CLOSURE variable — declared outside the lambda body, in the definer script's own top-level scope — IN PLACE (list.add, mirroring Variable_ListMutationInPlaceThrowsViaSampledWalk), dispatched repeatedly via InvokeAsExecution under a MaxVariableBytes the closure eventually exceeds. In-place mutation produces no new value at an assignment site, so only the periodic SAMPLED WALK (M3, VariableBudget.Observe/Measure) can catch it, and that walk terminates at VariableBudget's own captured 'root' - the property this test is actually pinned to. Must throw ScriptVariableLimitExceededException(Bytes); fails (times out without throwing) if InvokeAsExecution ever reallocates the VariableBudget instead of inheriting the captured one by reference, since a fresh budget rooted at the lambda's own per-dispatch arguments would both stop the walk BELOW the closure where $store lives AND lose the accumulated sample-tick count between dispatches (a fresh object every call never reaches the sampling interval). Verified by deliberately reverting the production change: fails cleanly (no throw within the loop) rather than crashing or hanging.")]
        public void Variable_ClosureListMutationAcrossInvokeAsExecutionDispatchesIsCharged() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 2000}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$store = new list()",
                "$f = []=>{ $store.add(\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\") }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => {
                for (int i = 0; i < 5000; i++)
                    lambda.InvokeAsExecution();
            });
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, MaxTime(2000)]
        [Description("DiVoid #7880 §16 R5 verification item (design §14 step 9): a lambda defined under a configured Timeout has a captured CancellationToken derived from a CancellationTokenSource that GuardedExecution.Dispose already tore down un-fired. InvokeAsExecution must never link to that captured token — dispatching afterwards must not throw ObjectDisposedException. This is the direct regression pin for A10: the hazard is real (CancellationToken.Register throws ObjectDisposedException when its source was disposed), it just isn't reachable through the design's actual shape, which this test proves by construction rather than by argument.")]
        public void InvokeAsExecution_DoesNotThrowObjectDisposedExceptionAfterDefinerTimeoutDisposed() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromSeconds(30)}
            };
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = $x=>{ return($x) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            object result = null;
            Assert.DoesNotThrow(() => result = lambda.InvokeAsExecution(CancellationToken.None, 7));
            Assert.That(result, Is.EqualTo(7));
        }

        /// <summary>
        /// compile-only regression guard for the two <see cref="LambdaMethod.InvokeAsExecution"/> overloads
        /// (DiVoid #7880 §4 — the signature-level half): a null literal, a statically-typed <see cref="CancellationToken"/>,
        /// an object-typed variable holding a boxed token, zero arguments and the intended two-argument call must
        /// all resolve unambiguously at compile time. Never called; a CS0121 here means the overload pair became
        /// ambiguous and needs a re-argued signature, not a silent fix
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        static void CompileOnly_InvokeAsExecutionOverloadsResolveUnambiguously(LambdaMethod lambda, CancellationToken token, object boxedToken, double delta) {
            lambda.InvokeAsExecution();
            lambda.InvokeAsExecution(null);
            lambda.InvokeAsExecution(token);
            lambda.InvokeAsExecution(boxedToken);
            lambda.InvokeAsExecution(token, delta);
        }

        [Test]
        [Description("DiVoid #7880 §4 — runtime half of CompileOnly_InvokeAsExecutionOverloadsResolveUnambiguously: a statically-typed CancellationToken argument must resolve to the (CancellationToken, params object[]) overload, not be boxed into the params array. Pre-cancelling the token makes the two resolutions observably different: the intended overload throws OperationCanceledException immediately (Guard() checks the token before anything else runs); the wrong overload would instead throw ScriptRuntimeException for an argument-count mismatch against this zero-parameter lambda.")]
        public void InvokeAsExecution_StaticallyTypedCancellationTokenResolvesToTokenOverload() {
            ScriptParser parser = new();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = []=>{ return(1) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            CancellationTokenSource cts = new();
            cts.Cancel();
            CancellationToken token = cts.Token;

            Assert.Throws<OperationCanceledException>(() => lambda.InvokeAsExecution(token));
        }

        [Test]
        [Description("DiVoid #7880 §4: an object-typed variable holding a boxed CancellationToken is NOT implicitly unboxed by overload resolution, so it resolves to the params-only overload and is passed through as an ordinary script argument rather than as the cancellation token — distinguishable here because the zero-parameter lambda then sees an unexpected argument.")]
        public void InvokeAsExecution_ObjectTypedTokenVariableResolvesToArgumentsOverload() {
            ScriptParser parser = new();
            IScript definitions = parser.Parse(ScriptCode.Create(
                "$f = []=>{ return(1) }",
                "$f"
            ));
            LambdaMethod lambda = (LambdaMethod) definitions.Execute(new VariableProvider());

            object boxedToken = CancellationToken.None;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => lambda.InvokeAsExecution(boxedToken));
            Assert.That(exception.Message, Does.Contain("Argument count"));
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
        [Description("Extends CancellationSupportTests.CF2_DefaultLimitsAreNotSharedMutableState to MaxDepth: a configured parser's depth ceiling must not leak onto a separately constructed default parser. Updated for secure-by-default (design §18): the default parser now carries ScriptLimits.Default, not ScriptLimits.None.")]
        public void CF2Extended_MaxDepthDefaultsToDefaultAndDoesNotLeakAcrossParsers() {
            ScriptParser configuredParser = new() {
                Limits = new ScriptLimits {MaxDepth = SafeMaxDepth - 3}
            };
            ScriptParser defaultParser = new();

            Assert.That(defaultParser.Limits, Is.SameAs(ScriptLimits.Default));
            Assert.That(defaultParser.Limits.MaxDepth, Is.EqualTo(ScriptLimits.DefaultMaxDepth));
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

        /// <summary>
        /// host object that owns a sizeable internal payload but does not itself present as a string, array,
        /// <see cref="System.Collections.IDictionary"/> or <see cref="System.Collections.ICollection"/>, so
        /// <see cref="VariableSizer"/> charges it a flat opaque constant instead of walking into it
        /// </summary>
        class OpaqueHostObject {
            public byte[] Payload { get; } = new byte[10_000_000];
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §8.2.4/T18a - the load-bearing peak-vs-sustained proof: $s = $s + $s doubles within one statement, so M1 (the per-produced-value ceiling at ValueOperation.ExecuteToken/AssignableToken.Assign) must abort on the first value that crosses the budget, not after the sampled walk eventually notices. The measured value at the throw must stay within a small multiple of the limit regardless of how many doublings a sampling-only design would have allowed.")]
        public void Variable_AssignmentDoublingAbortsBeforeMultipleOfLimit() {
            const long limit = 200;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = limit}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$s = \"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"",
                "while(true) {",
                "  $s = $s + $s",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(exception.Measured, Is.LessThanOrEqualTo(2 * limit));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [TestCase(4)]
        [TestCase(6400)]
        [Description("Design §8.2.4 chain verdict/T18b: a long operator chain in one statement ($s+$s+...+$s) trips M1 at the first intermediate that crosses the budget, not at the n-th term - the measured value at the throw must be independent of chain length n, never scaling with the number of terms.")]
        public void Variable_LongConcatenationChainAbortsIndependentOfChainLength(int termCount) {
            const long limit = 200;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = limit}
            };
            string chain = string.Join("+", Enumerable.Repeat("$s", termCount));
            IScript script = parser.Parse(ScriptCode.Create(
                "$result = " + chain
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(
                () => script.Execute(new VariableProvider(new Variable("s", new string('x', 50)))));
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(exception.Measured, Is.LessThanOrEqualTo(2 * limit));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §8.2.2/T10 - the shape M1/M2 cannot see: $l.add(...) mutates the list in place without producing a new value at an operator or assignment site, so only the sampled walk (M3, at Guard()) can catch the growth.")]
        public void Variable_ListMutationInPlaceThrowsViaSampledWalk() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 2000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "while(true) {",
                "  $l.add(\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\")",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §8.4 - the host-usage boundary: a variable holding a host object graph is charged VariableSizer's flat opaque constant, not walked into it, so a low MaxVariableBytes does not abort a script that merely references a large host-owned object it did not itself allocate.")]
        public void Variable_HostObjectGraphChargedOpaqueConstantNotWalked() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 1000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$x = $obj",
                "$y = 1"
            ));

            Assert.DoesNotThrow(() => script.Execute(new VariableProvider(new Variable("obj", new OpaqueHostObject()))));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7809 W1/T18d: repeated small, separately-produced values (each under the byte ceiling on its own, so M1 never fires) accumulated via in-place mutation must trip M2's forced measurement pass well ahead of the 256-tick sampled floor. MaxSteps is set below that floor, so without M2 forcing an early pass the step budget - not the variable budget - would be what throws.")]
        public void Variable_SeparatelyProducedValuesForcesMeasurementViaM2() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 1000, MaxSteps = 200}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "while(true) {",
                "  $x = \"xxxxxxxxxxxxxxxxxxxx\"",
                "  $l.add($x)",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7809 W2: a real §8.4 root-exclusion pin, distinct from the §8.5 opaque-charging pin above - a plain large string in the host-supplied root variable set is sized precisely by VariableSizer (it is not an opaque host type), so this only passes if the root scope itself is excluded from the walk, not merely because the value type is unrecognised.")]
        public void Variable_HostRootLargeStringExcludedFromBudget() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 1000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "for($i=0,$i<300,++$i) {",
                "  $y = 1",
                "}"
            ));

            Assert.DoesNotThrow(() => script.Execute(new VariableProvider(new Variable("bigdata", new string('x', 1_000_000)))));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §8.3 - the scope-death trap: while(true) { $x = 1 } re-declares $x in a fresh block scope every iteration, so live entry count never grows even after many iterations. An incremental declaration counter would false-positive here; a walk over live scopes must not.")]
        public void Variable_ScopeDeathDoesNotFalsePositiveOnEntryCount() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariables = 3, MaxSteps = 5000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "while(true) {",
                "  $x = 1",
                "}"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §8.3/T13: MaxVariables catches live entries accumulated in a scope that stays alive across many checkpoints, distinct from MaxVariableBytes - the light tier's own reachable shape.")]
        public void Variable_EntriesLimitThrowsWithEntriesKind() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariables = 5}
            };
            List<string> lines = new();
            for (int i = 0; i < 10; i++)
                lines.Add($"$v{i} = {i}");
            lines.Add("for($i=0,$i<1000,++$i) {");
            lines.Add("  $tmp = 1");
            lines.Add("}");
            IScript script = parser.Parse(ScriptCode.Create(lines.ToArray()));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Entries));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §8.2.2 - the hook belongs on AssignableToken.Assign, the base class, since += does not route through ValueOperation.ExecuteToken at all.")]
        public void Variable_CompoundAssignHooksAssignableTokenBase() {
            const long limit = 200;
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = limit}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$s = \"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"",
                "while(true) {",
                "  $s += $s",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7836/#7837: new list(capacity) assigned and held allocates its backing array immediately, so VariableSizer must charge Capacity rather than the Count=0 the list reports right after construction - closes the memory-guard blind spot the round-4 red-team found.")]
        public void Variable_PreSizedEmptyListChargedByCapacityAbortsAtAssignment() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 1000}
            };
            IScript script = parser.Parse("$a = new list(1000)");

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7836 4A2: three retained pre-sized lists, none individually over budget, must still abort once their accumulated capacity crosses MaxVariableBytes - pins that the capacity charge is retained and summed, not a one-shot check that forgets the earlier variables.")]
        public void Variable_RetainedPreSizedListsAccumulateAndAbort() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 1500}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = new list(100)",
                "$b = new list(100)",
                "$c = new list(100)"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7837 acceptance: a legitimately small pre-sized list under the configured budget must still complete - the capacity charge must not false-positive on ordinary pre-sizing.")]
        public void Variable_SmallPreSizedListUnderBudgetCompletes() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 10_000}
            };
            IScript script = parser.Parse("$a = new list(100)");

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7877 §10.3: a host method that doubles a script-held list in host C# is charged by neither M1 (void return), M4 (not a capacity operation) nor the tick cadence (~64 checkpoints against a 256 floor) - only the allocation-denominated growth trigger catches it.")]
        public void Variable_HostGrowthPrimitiveOutrunsTickCadenceButNotGrowthTrigger() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 1_000_000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "$l.add(1)",
                "for($i=0,$i<20,++$i) {",
                "  $grower.grow($l)",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(
                () => script.Execute(new VariableProvider(new Variable("grower", new ListGrowerHost()))));
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(exception.Measured, Is.LessThan(16 * 1_000_000L));
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("Design §8/§13: the growth trigger only forces a measurement pass and must never itself throw - a loop producing far more transient allocation than MaxVariableBytes while retaining a single small variable must still complete.")]
        public void Variable_ChurnUnderGrowthTriggerDoesNotFalsePositive() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 100_000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$i = 0",
                "while($i < 50000) {",
                "  $tmp = \"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"",
                "  $i = $i + 1",
                "}"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §10.4: written behaviourally through the public surface rather than InternalsVisibleTo (§2, #114 2026-08-08 ruling) - a ScriptLimits.None parser running the same host-growth script that throws under a configured budget must complete normally, since no VariableBudget exists to enforce anything.")]
        public void Variable_UnconfiguredHostCompletesGrowthPrimitiveThatWouldBreachConfiguredBudget() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "$l.add(1)",
                "for($i=0,$i<20,++$i) {",
                "  $grower.grow($l)",
                "}"
            ));

            Assert.DoesNotThrow(() => script.Execute(new VariableProvider(new Variable("grower", new ListGrowerHost()))));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("Design §7 row 2/§10.4: MaxVariables alone must still throw on entry count via the unchanged M3 walk, since the growth trigger is gated on MaxVariableBytes being configured.")]
        public void Variable_EntriesOnlyHostUnaffectedByGrowthTrigger() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariables = 5}
            };
            List<string> lines = new();
            for (int i = 0; i < 10; i++)
                lines.Add($"$v{i} = {i}");
            lines.Add("for($i=0,$i<1000,++$i) {");
            lines.Add("  $tmp = 1");
            lines.Add("}");
            IScript script = parser.Parse(ScriptCode.Create(lines.ToArray()));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Entries));
        }

        [Test]
        [Description("Direct unit test for VariableSizer's capacity charge (DiVoid #7836/#7837): a pre-sized, still-empty List<object> must be sized by its allocated Capacity, not its live Count of zero.")]
        public void VariableSizer_PreSizedEmptyListChargedByCapacityNotCount() {
            List<object> preSized = new(1_000_000);

            long size = VariableSizer.Size(preSized);

            Assert.That(size, Is.GreaterThanOrEqualTo(1_000_000L * 8));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("A script-level try/catch around a variable-limit breach must not swallow it, the same contract already pinned for depth breaches.")]
        public void Try_DoesNotSwallowVariableLimitAbort() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 200}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$s = \"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"",
                "try {",
                "  while(true) {",
                "    $s = $s + $s",
                "  }",
                "} catch {",
                "  $flag.Caught = true",
                "}"
            ));

            MutableFlag flag = new();
            Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute(new VariableProvider(new Variable("flag", flag))));
            Assert.That(flag.Caught, Is.False);
        }

        [Test]
        [Description("T31b's variable-guard counterpart (design §12 S2): a default execution with neither MaxVariables nor MaxVariableBytes configured must not allocate a VariableBudget at all, keeping Guard()'s added line a single null-conditional no-op.")]
        public void Variable_DefaultExecutionAllocatesNoVariableBudget() {
            using GuardedExecution execution = GuardedExecution.Prepare(new VariableProvider(), null, CancellationToken.None, ScriptLimits.None);

            Assert.That(execution.Context.VariableBudget, Is.Null);
        }

        [Test]
        [Description("Direct unit test for VariableSizer's depth cap doubling as a cycle guard (design §8.5): a list containing itself must terminate sizing rather than recurse indefinitely.")]
        public void VariableSizer_SelfReferencingListTerminates() {
            List<object> self = new();
            self.Add(self);

            Assert.DoesNotThrow(() => VariableSizer.Size(self));
        }

        /// <summary>
        /// non-<see cref="System.Collections.ICollection"/> <see cref="IEnumerable"/> that would enumerate
        /// forever if <see cref="VariableSizer"/> ever iterated it
        /// </summary>
        class InfiniteEnumerable : IEnumerable {
            public IEnumerator GetEnumerator() {
                while (true)
                    yield return 1;
            }
        }

        [Test]
        [Description("Direct unit test for VariableSizer never enumerating a non-ICollection IEnumerable (design §8.5): an infinite host sequence held in a variable must be sized as an opaque constant, not enumerated.")]
        public void VariableSizer_InfiniteEnumerableNeverEnumerated() {
            Assert.DoesNotThrow(() => VariableSizer.Size(new InfiniteEnumerable()));
        }

        [Test, MaxTime(2000)]
        [Description("T41 (design §8.8.6, Instance I): new list(100000000) assigned on a bare new ScriptParser() throws ScriptVariableLimitExceededException with a total-process allocation delta of a few MB, not the ~800MB the requested capacity would spike to if the throw fired after constructor.Invoke rather than before it - the load-bearing proof that M4's pre-allocation charge precedes the allocation.")]
        public void T41_PreSizedListAssignedThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$a = new list(100000000)");

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
        }

        [Test]
        [Description("T42 (design §8.8.6): direct unit test for VariableBudget.ChargePreAllocation - refuses the projected bytes of a List<> capacity ctor before any List<object> is actually constructed, pinning that Site A's charge precedes constructor.Invoke with zero allocation observed. Closes residual #7840 for the reachable ctor.")]
        public void T42_ChargePreAllocationRefusesListCtorProjectionWithoutConstructing() {
            VariableBudget budget = new(new VariableProvider(), null, 128L * 1024 * 1024);
            ConstructorInfo ctor = typeof(List<object>).GetConstructor(new[] {typeof(int)});
            Assert.That(VariableSizer.TryGetPreAllocationOperation(null, ctor, new object[] {2_000_000_000}, out long projected), Is.True);

            bool allocated = false;

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => {
                budget.ChargePreAllocation(projected);
                allocated = true;
            });

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(allocated, Is.False);
        }

        [Test, MaxTime(2000)]
        [Description("T43 (design §8.8.6, Instance II): $a.ensurecapacity(100000000) on a bare parser must throw before the backing array is allocated - the mutating-method vector no M1/M2 produced-value charge and no M3 sampled walk can catch inside a short script.")]
        public void T43_EnsureCapacityOnEmptyListThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = new list()",
                "$a.ensurecapacity(100000000)"
            ));

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
        }

        [Test, MaxTime(2000)]
        [Description("T44 (design §8.8.6, Instance II): $a.capacity = 100000000 on a bare parser must throw before the backing array is allocated - the Capacity-setter vector.")]
        public void T44_CapacitySetterOnEmptyListThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = new list()",
                "$a.capacity = 100000000"
            ));

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
        }

        [Test, MaxTime(2000)]
        [Description("T45 (design §8.8.6, Instance III): $d.ensurecapacity(30000000) on a bare parser must throw before the backing arrays are allocated - the dictionary vector the #7839 sizer fix alone cannot close, since Dictionary<,> exposes no public Capacity getter for the sampled walk to see.")]
        public void T45_DictionaryEnsureCapacityThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$d = {\"x\":1}",
                "$d.ensurecapacity(30000000)"
            ));

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
        }

        [Test, MaxTime(2000)]
        [Description("T46 (design §8.8.6): legitimate small pre-sizes under budget - a capacity ctor, EnsureCapacity on a list and a dict, and a Capacity-setter - must all complete without a false abort, pinning that M4 gates on projected bytes, not on the mere presence of a capacity operation.")]
        public void T46_LegitSmallCapacityOperationsCompleteWithoutFalseAbort() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxVariableBytes = 10_000_000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = new list(1000)",
                "$a.ensurecapacity(1000)",
                "$a.capacity = 1000",
                "$d = {\"x\":1}",
                "$d.ensurecapacity(100)"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test]
        [Description("T47 (design §8.8.4/§8.8.6): direct VariableSizer pin for both the #7839 List<> capacity path and the new Dictionary<,> footprint path - a pre-sized-but-empty List<object> is charged by its allocated Capacity, and a Dictionary<object,object> after EnsureCapacity(n) is charged by its bucket/entry footprint, not its live Count of near-zero. Fails loudly if either private/public capacity accessor breaks across a runtime. Runs on net8.0 here (the only TFM this test project executes against); the netstandard2.0 build of VariableSizer is compile-verified by the Release build - the candidate field names it tries at runtime (_entries/entries) are resolved against whichever CLR actually loads the assembly, not the TFM it was compiled for.")]
        public void T47_VariableSizerChargesListAndDictionaryCapacityNotCount() {
            List<object> preSizedList = new(100_000_000);
            long listSize = VariableSizer.Size(preSizedList);
            Assert.That(listSize, Is.GreaterThanOrEqualTo(100_000_000L * VariableSizer.CollectionElementOverhead));

            Dictionary<object, object> preSizedDictionary = new();
            preSizedDictionary.EnsureCapacity(30_000_000);
            long dictionarySize = VariableSizer.Size(preSizedDictionary);
            long countChargedSize = preSizedDictionary.Count * VariableSizer.DictionaryEntryOverhead;

            Assert.That(dictionarySize, Is.GreaterThanOrEqualTo(30_000_000L * VariableSizer.DictionaryEntryOverhead));
            Assert.That(dictionarySize, Is.GreaterThan(countChargedSize + 1000));
        }

        [Test, MaxTime(5000)]
        [Description("T48 (design §8.8.5): charge-once regression - new list(10000000) (~80MB, under the default 128MiB budget) completes, then 300 trivial iterations force an M3 Measure pass, then a further ~40MB variable also completes - all without a spurious abort. If M4's producedSinceLastPass charge were not reset by that Measure pass, the forced walk would see the list's own 80MB twice (~160MB, over budget) and throw falsely.")]
        public void T48_ChargeOnceRegressionPreSizedValueNotDoubleCounted() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = new list(10000000)",
                "for($i=0,$i<300,++$i) {",
                "  $y = 1",
                "}",
                "$b = \"" + new string('x', 20_000_000) + "\""
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T49 (design §8.8.6): characterisation test - new dictionary(100000000) is still a ScriptRuntimeException naming 'matching constructor', unchanged by M4. dictionary is registered as the IDictionary interface (ScriptParser.cs), which has no constructors, so the Dictionary<,> ctor row in the capacity-operation table stays future-proofing (design §8.8.4), not a reachable path today.")]
        public void T49_DictionaryCtorRemainsUnreachableCharacterisation() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$d = new dictionary(100000000)");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.Message, Does.Contain("matching constructor"));
        }

        [Test, MaxTime(2000)]
        [Description("T50 (design §8.1 row 1, DiVoid #7854 Category A/#7868): \"a\".padright(100000000) assigned on a bare new ScriptParser() throws ScriptVariableLimitExceededException with an allocation delta of a few MB, not the ~200MB spike a post-hoc charge would let through - the pre-allocation projection precedes String.PadRight's own allocation.")]
        public void T50_PadRightAssignedThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$s = \"a\".padright(100000000)");

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
        }

        [Test, MaxTime(2000)]
        [Description("T51 (design §8.1 rows 1-2): padleft, padright with an explicit padding character, and an unassigned padright expression all throw ScriptVariableLimitExceededException before allocation on a bare parser - pins the charge for every PadRight/PadLeft overload, regardless of assignment.")]
        public void T51_PadVariantsAndUnassignedExpressionThrowBeforeAllocation() {
            ScriptParser parser = new();

            Assert.Throws<ScriptVariableLimitExceededException>(() => parser.Parse("$s = \"a\".padleft(100000000)").Execute());
            Assert.Throws<ScriptVariableLimitExceededException>(() => parser.Parse("$s = \"a\".padright(100000000,'b')").Execute());
            Assert.Throws<ScriptVariableLimitExceededException>(() => parser.Parse("\"a\".padright(100000000)").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T52 (design §8.1 row 3): new string('a',100000000) throws ScriptVariableLimitExceededException before allocation, pinning the arg-index-1 projection through TypeInstanceProvider.Create.")]
        public void T52_CharCtorAssignedThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$s = new string('a',100000000)");

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
        }

        [Test, MaxTime(2000)]
        [Description("T53 (design §8.1 rows 1-3): small, legitimate pad and new string(char,int) calls under the default byte budget complete without a false abort.")]
        public void T53_SmallPadAndCharCtorCompleteWithoutFalseAbort() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = \"a\".padright(1000)",
                "$b = \"ab\".padleft(10,'x')",
                "$c = new string('a',1000)"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T74 (design §8.1 rows 7-8, §10.3): List<>.AddRange with a lazy, non-ICollection argument is refused rather than silently unbounded, naming '.toarray()' as the workaround.")]
        public void T74_AddRangeWithNonCollectionArgumentIsRefused() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "$l.add(1)",
                "$other = $l.where($x=>true)",
                "$l.addrange($other)"
            ));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.Message, Does.Contain(".toarray()"));
        }

        [Test, MaxTime(2000)]
        [Description("T54 (design §9.1 R1/R2, DiVoid #7868): tostring with an oversized standard-format precision throws ScriptRuntimeException before allocation on every affected numeric primitive, including P (round-8's correction).")]
        public void T54_ToStringOversizedPrecisionThrowsAcrossPrimitives() {
            ScriptParser parser = new();

            long before = GC.GetTotalAllocatedBytes(true);
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (1).tostring(\"D100000000\")").Execute());
            long delta = GC.GetTotalAllocatedBytes(true) - before;
            Assert.That(delta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));
            Assert.That(exception, Is.Not.Null);

            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (1.0).tostring(\"F100000000\")").Execute());
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (1).tostring(\"N50000000\")").Execute());
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (1).tostring(\"X100000000\")").Execute());
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (1.0).tostring(\"P90000000\")").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T55 (design §9.4 form b): the ':' format operator rewrites to the same ScriptMethod node as the method-call form, so 1:D100000000 is governed identically.")]
        public void T55_FormatOperatorFormIsGoverned() {
            ScriptParser parser = new();
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = 1:D100000000").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T56 (design §9.4 form c): a ':' format hole inside string interpolation hits the same rewrite as the method-call form, so a guard covering only the method call would leave this green-and-wrong.")]
        public void T56_InterpolationFormatHoleIsGoverned() {
            ScriptParser parser = new();
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = $\"{1:D100000000}\"").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T57 (design §9.5, A1/A2): the format check runs on the runtime VALUE of the argument, not the source literal - a runtime-computed digit count and a format string carried through a variable are both governed.")]
        public void T57_FormatCheckAppliesToRuntimeComputedValue() {
            ScriptParser parser = new();
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse(ScriptCode.Create(
                "$n = 50000000 * 2",
                "$a = (1).tostring(\"D\" + $n)"
            )).Execute());
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse(ScriptCode.Create(
                "$w = \"D100000000\"",
                "$a = (1).tostring($w)"
            )).Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T58 (design §6.1, A2): the format check runs pre-cache - a benign tostring call followed by a hostile one sharing the same (type,name,argtype) cache key still throws on the second call, proving the check is not short-circuited by the resolution cache.")]
        public void T58_FormatCheckAppliesAfterCacheWarm() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = (1).tostring(\"F2\")",
                "$b = (1).tostring(\"D100000000\")"
            ));
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T59 (design §9.1 R0/R3): the everyday ToString/format vocabulary - no arguments, a small standard specifier, interpolation, the format operator - is untouched by the format policy.")]
        public void T59_EverydayFormatVocabularyCompletesWithoutFalseAbort() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$a = (1.5).tostring()",
                "$b = (1.5).tostring(\"F2\")",
                "$c = $\"{1234.5:N0}\"",
                "$d = (255).tostring(\"X8\")",
                "$e = 1:F2",
                "$f = (1).tostring(\"G\")",
                "$g = (1.0).tostring(\"R\")"
            ));
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test]
        [Description("T60 (design §10.3): MethodGuard.IsAllowed refuses a method absent from a tier-1 type's generated inventory - a direct unit pin, since the checked-in inventory is derived from the type's full current runtime surface (T78's zero-configuration guarantee), so no legitimate script call against a fully-current table can exercise this path end-to-end.")]
        public void T60_GovernedTypeMethodAbsentFromInventoryIsRefused() {
            MethodGuard guard = new();
            MethodInfo foreignMethod = typeof(List<object>).GetMethod(nameof(List<object>.Sort), Type.EmptyTypes);
            Assert.That(guard.IsAllowed(typeof(string), foreignMethod), Is.False);
        }

        [Test]
        [Description("T61 (design §10.2): parser.Methods.Allow<T> widens the tier-1 inventory for the named method, flipping IsAllowed's verdict for exactly that (type,name) pair.")]
        public void T61_AllowWidensTier1Inventory() {
            MethodGuard guard = new();
            MethodInfo foreignMethod = typeof(List<object>).GetMethod(nameof(List<object>.Sort), Type.EmptyTypes);
            Assert.That(guard.IsAllowed(typeof(string), foreignMethod), Is.False);

            guard.Allow<string>("sort");
            Assert.That(guard.IsAllowed(typeof(string), foreignMethod), Is.True);
        }

        [Test, MaxTime(2000)]
        [Description("T62 (design §10.2): parser.Methods.Ungovern<T> restores a type's full public surface, including bypassing the format policy - the trusted-host escape hatch.")]
        public void T62_UngovernRestoresFullSurfaceIncludingFormatPolicy() {
            ScriptParser parser = new();
            parser.Methods.Ungovern<double>();
            IScript script = parser.Parse("$a = (1.0).tostring(\"F20\")");
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T63 (design §10.2, A13): Ungovern on one parser's MethodGuard does not leak to a second parser's format policy - per-parser isolation.")]
        public void T63_UngovernDoesNotLeakBetweenParsers() {
            ScriptParser ungoverned = new();
            ungoverned.Methods.Ungovern<double>();
            ScriptParser governed = new();

            Assert.DoesNotThrow(() => ungoverned.Parse("$a = (1.0).tostring(\"F20\")").Execute());
            Assert.Throws<ScriptRuntimeException>(() => governed.Parse("$a = (1.0).tostring(\"D100000000\")").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T64 (design §6.2): registered extension methods bypass the tier-1 allow-list by construction - AddExtensions is itself the opt-in, so the common EnumerableExtensions surface keeps working unlisted.")]
        public void T64_ExtensionMethodsBypassAllowList() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "$l.add(1)",
                "$l.add(2)",
                "$l.where($x=>$x>0).count()"
            ));
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test]
        [Description("T66 (design §9.1-§9.2): direct unit pins for MethodGuard.IsAcceptableFormat across the exhaustive shape/length boundary set, without the engine in the loop.")]
        public void T66_IsAcceptableFormatBoundaryPins() {
            Assert.That(MethodGuard.IsAcceptableFormat(""), Is.True);
            Assert.That(MethodGuard.IsAcceptableFormat("G"), Is.True);
            Assert.That(MethodGuard.IsAcceptableFormat("F2"), Is.True);
            Assert.That(MethodGuard.IsAcceptableFormat("D99"), Is.True);
            Assert.That(MethodGuard.IsAcceptableFormat("D100"), Is.False);
            Assert.That(MethodGuard.IsAcceptableFormat("P90000000"), Is.False);
            Assert.That(MethodGuard.IsAcceptableFormat("X8"), Is.True);
            Assert.That(MethodGuard.IsAcceptableFormat(new string('0', MethodGuard.MaxFormatLength)), Is.True);
            Assert.That(MethodGuard.IsAcceptableFormat(new string('0', MethodGuard.MaxFormatLength + 1)), Is.False);
            Assert.That(MethodGuard.IsAcceptableFormat(null), Is.True);
        }

        [Test, MaxTime(2000)]
        [Description("T67 (design §10.3): the ToString format-denial message truncates the echoed format string to 32 characters, keeping the exception message bounded regardless of the format argument's own length.")]
        public void T67_FormatDenialMessageIsBounded() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$a = (1).tostring(new string('D',100000))");
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.Message.Length, Is.LessThan(512));
        }

        [Test]
        [Description("T68 (design §7.3): re-runs the tier-1 closure/inventory derivation against the running runtime and compares it to the checked-in MethodGuard table - fails loudly the day a .NET upgrade changes the reflected surface the table was generated from.")]
        public void T68_AllowListMatchesGeneratedInventory() {
            Dictionary<Type, HashSet<string>> regenerated = MethodGuardInventoryGenerator.ComputeInventory();
            IReadOnlyDictionary<Type, HashSet<string>> checkedIn = MethodGuard.GeneratedInventory;

            Assert.That(regenerated.Keys, Is.EquivalentTo(checkedIn.Keys));
            foreach (Type type in regenerated.Keys)
                Assert.That(regenerated[type], Is.EquivalentTo(checkedIn[type]), $"drift on {type}");
        }

        [Test, MaxTime(2000)]
        [Description("T72 (design §9.4, A19): the 2-arg ToString(format, IFormatProvider) overload reachable with a literal null provider is governed by the same format check - arity-independent.")]
        public void T72_TwoArgToStringOverloadWithNullProviderIsGoverned() {
            ScriptParser parser = new();
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (1).tostring(\"D100000000\", null)").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T73 (design §9.1 R1, double coverage): a huge custom format string never reaches the format policy because building it is refused first, by the string pre-allocation charge (design §8.1 row 3).")]
        public void T73_CustomFormatAmplifierIsDeniedByPreAllocationCharge() {
            ScriptParser parser = new();
            Assert.Throws<ScriptVariableLimitExceededException>(() => parser.Parse("$a = (1).tostring(new string('0',80000000))").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T75 (design §7.6.2, A22): Char and Boolean expose no public ToString(string,...) overload on this TFM, so A22's predicted over-inclusion - IsFormatFamily may still be true via an explicit ISpanFormattable/IFormattable implementation Public|Instance binding never surfaces - costs nothing: an oversized format value on either type is always refused, whether by the format policy itself or by resolution/conversion failing on the mismatched overload, never by a successful oversized ToString.")]
        public void T75_OverIncludedFormattableTypesNeverProduceOversizedToString() {
            Assert.That(typeof(char).GetMethod("ToString", new[] {typeof(string)}), Is.Null);
            Assert.That(typeof(bool).GetMethod("ToString", new[] {typeof(string)}), Is.Null);

            ScriptParser parser = new();
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = ('x').tostring(\"D100000000\")").Execute());
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("$a = (true).tostring(\"D100000000\")").Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T76 (design §7.6.2): H1 governs a host-injected IFormattable property (DateTime) - a hand-written 11-primitive list would have missed this entirely.")]
        public void T76_HostInjectedFormattableIsGovernedByFormatPolicy() {
            ScriptParser parser = new();
            parser.Types.AddType<FormattableHost>("formattablehost");
            IScript script = parser.Parse(ScriptCode.Create(
                "$h = new formattablehost()",
                "$a = $h.when.tostring(\"D100000000\")"
            ));
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T77 (design §7.6.3): H2 refuses an integral argument whose value alone exceeds the budget on a host-injected member, before the member's own body runs; a small, legitimate value completes.")]
        public void T77_H2RefusesOversizedIntegralArgumentOnHostMember() {
            ScriptParser parser = new();
            parser.Types.AddType<RepeatingHost>("repeatinghost");

            IScript hostile = parser.Parse(ScriptCode.Create(
                "$h = new repeatinghost()",
                "$a = $h.repeat(2000000000)"
            ));
            Assert.Throws<ScriptVariableLimitExceededException>(() => hostile.Execute());

            IScript benign = parser.Parse(ScriptCode.Create(
                "$h = new repeatinghost()",
                "$a = $h.repeat(1000)"
            ));
            Assert.DoesNotThrow(() => benign.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T78 (design §7.6.4, the usability gate): an ordinary host domain type with ten zero-arg getters, SetName(string), Find(filter) and GetById(int) all complete with zero parser.Methods.Allow/Charge/Deny configuration - H3 permit-by-shape. If this fails the model is unusable and the design must be bounced.")]
        public void T78_OrdinaryDomainTypeWorksWithZeroHostConfiguration() {
            ScriptParser parser = new();
            parser.Types.AddType<OrdinaryDomainHost>("host");
            IScript script = parser.Parse(ScriptCode.Create(
                "$h = new host()",
                "$a = $h.getname()",
                "$b = $h.getage()",
                "$c = $h.getactive()",
                "$d = $h.getscore()",
                "$e = $h.getdescription()",
                "$f = $h.getcategory()",
                "$g = $h.getrank()",
                "$i = $h.getverified()",
                "$j = $h.getweight()",
                "$k = $h.gettag()",
                "$h.setname(\"y\")",
                "$m = $h.find($h)",
                "$n = $h.getbyid(5)"
            ));
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T79 (design §10.2, residual 11): a host Charge<T> row overrides H2's one-byte-per-unit guess with the host's own known per-unit cost, refusing before the call at a value H2 alone would have admitted.")]
        public void T79_HostChargeRowOverridesH2Guess() {
            ScriptParser parser = new();
            parser.Types.AddType<BufferHost>("bufferhost");
            parser.Methods.Charge<BufferHost>("makebuffers", 0, 100);
            IScript script = parser.Parse(ScriptCode.Create(
                "$h = new bufferhost()",
                "$a = $h.makebuffers(100000000)"
            ));
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, MaxTime(2000)]
        [Description("T80 (design §7.6.5): a host Deny<T> entry refuses a zero-argument host member no shape rule can see.")]
        public void T80_DenyRefusesZeroArgumentHostAllocator() {
            ScriptParser parser = new();
            parser.Types.AddType<RenderHost>("renderhost");
            parser.Methods.Deny<RenderHost>("render");
            IScript script = parser.Parse(ScriptCode.Create(
                "$h = new renderhost()",
                "$a = $h.render()"
            ));
            Assert.Throws<ScriptRuntimeException>(() => script.Execute());
        }

        [Test, MaxTime(2000)]
        [Description("T81 (design §10.2, A13): host Charge/Deny rules registered on one parser's MethodGuard do not leak to a second parser sharing the same host type - per-parser isolation for the new tier-2 APIs too.")]
        public void T81_ChargeAndDenyDoNotLeakBetweenParsers() {
            ScriptParser configured = new();
            configured.Types.AddType<RenderHost>("renderhost");
            configured.Methods.Deny<RenderHost>("render");

            ScriptParser bare = new();
            bare.Types.AddType<RenderHost>("renderhost");

            Assert.Throws<ScriptRuntimeException>(() => configured.Parse("$a = new renderhost().render()").Execute());
            Assert.DoesNotThrow(() => bare.Parse("$a = new renderhost().render()").Execute());
        }

        [Test]
        [Description("T82 (operator review, array-receiver classification bug): char[]/string[] - handed to script by String.ToCharArray/Split, both tier-1 members - must themselves be tier-1, not fall through to tier-2's default-allow. Direct unit pin: a method foreign to both types is refused. Fails if the fix is reverted (arrays stripped to element type, or never added to the generated table), since IsAllowed would then silently default-permit every array member via IsPermittedByShape.")]
        public void T82_ArrayReceiversAreTier1NotDefaultAllowedByTier2() {
            MethodGuard guard = new();
            Assert.That(guard.IsTier1(typeof(char[])), Is.True);
            Assert.That(guard.IsTier1(typeof(string[])), Is.True);

            MethodInfo foreignMethod = typeof(List<object>).GetMethod(nameof(List<object>.Sort), Type.EmptyTypes);
            Assert.That(guard.IsAllowed(typeof(char[]), foreignMethod), Is.False);
            Assert.That(guard.IsAllowed(typeof(string[]), foreignMethod), Is.False);
        }

        [Test, MaxTime(2000)]
        [Description("T83 (operator review, array-receiver classification bug): the arrays String.ToCharArray/Split hand to script complete their real, curated surface end-to-end on a bare parser - governing array receivers by identity does not break the everyday char[]/string[] vocabulary.")]
        public void T83_ArrayReceiverRealSurfaceCompletesOnBareParser() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$chars = \"abc\".tochararray()",
                "$parts = \"a,b\".split(\",\")"
            ));
            Assert.DoesNotThrow(() => script.Execute());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    /// exercises the complete cancellation-coverage and execution-guards design (docs/architecture/cancellation-support.md);
    /// every cancellation test carries a <see cref="MaxTimeAttribute"/> at least 20x shorter than the script's nominal
    /// runtime, so a pass is evidence of interruption rather than of a short sleep
    /// </summary>
    [TestFixture, Parallelizable]
    public class CancellationSupportTests {

        /// <summary>
        /// disposable which always fails to dispose, used to prove a dispose failure never replaces an in-flight exception
        /// </summary>
        class ThrowingDisposable : IDisposable {
            public void Dispose() => throw new InvalidOperationException("dispose failed");
        }

        /// <summary>
        /// disposable which records whether it was disposed, used to prove the worker actually unwinds on cancel
        /// </summary>
        class RecordingDisposable : IDisposable {
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }

        /// <summary>
        /// variable provider that signals a <see cref="ManualResetEventSlim"/> the moment the engine resolves
        /// any variable through it, used to observe when a worker has actually started running the script body
        /// rather than assuming it from elapsed wall-clock time
        /// </summary>
        class SignalingVariableProvider : IVariableProvider {
            readonly VariableProvider inner;
            readonly ManualResetEventSlim resolved;

            public SignalingVariableProvider(ManualResetEventSlim resolved, params Variable[] variables) {
                this.resolved = resolved;
                inner = new VariableProvider(variables);
            }

            public object this[string name] {
                get => inner[name];
                set => inner[name] = value;
            }

            public object GetVariable(string name) => inner.GetVariable(name);
            public bool ContainsVariable(string name) => inner.ContainsVariable(name);
            public bool ContainsVariableInHierarchy(string name) => inner.ContainsVariableInHierarchy(name);

            public IVariableProvider GetProvider(string variable) {
                resolved.Set();
                return inner.GetProvider(variable);
            }

            public IEnumerable<string> Variables => inner.Variables;
        }

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

        static IEnumerable<int> InfiniteSequence() {
            int i = 0;
            while (true)
                yield return i++;
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T1_TightLoopCancels() {
            ScriptParser parser = new();
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
        public async Task T2_LongBlockingWaitCancels() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "while(true) {",
                "  wait(60000)",
                "}"
            ));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T3_TryCatchAroundTightLoopDoesNotSwallowCancellation() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "try {",
                "  while(true) {",
                "    $x = 1",
                "  }",
                "} catch($e) {",
                "  $caught = true",
                "}"
            ));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
            Assert.That(task.IsCompletedSuccessfully, Is.False);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T4_TryCatchAroundLongWaitDoesNotSwallowCancellation() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "try {",
                "  wait(60000)",
                "} catch($e) {",
                "  $caught = true",
                "}"
            ));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
            Assert.That(task.IsCompletedSuccessfully, Is.False);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T5_AwaitNeverCompletingTaskCancels() {
            ScriptParser parser = new();
            IScript script = parser.Parse("await($never)");

            TaskCompletionSource<object> never = new();
            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(new VariableProvider(new Variable("never", never.Task)), cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T6_WaitAllOnNeverCompletingTaskCancels() {
            ScriptParser parser = new();
            IScript script = parser.Parse("task.waitall([$never])");

            TaskCompletionSource<object> never = new();
            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(new VariableProvider(new Variable("task", new TaskHost()), new Variable("never", never.Task)), cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T7a_EnumerableWithLambdaCancels() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.where($x=>$x>0).count()");

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(new VariableProvider(new Variable("src", InfiniteSequence())), cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T7b_EnumerableWithoutLambdaCancels() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.count()");

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(new VariableProvider(new Variable("src", InfiniteSequence())), cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void T8_CatastrophicRegexIsBoundedByMatchTimeout() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {RegexTimeout = TimeSpan.FromMilliseconds(100)}
            };
            IScript script = parser.Parse("\"aaaaaaaaaaaaaaaaaaaaaaaaaaaa!\" ~~ \"^(a+)+$\"");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.InnerException, Is.InstanceOf<RegexMatchTimeoutException>());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void T9_SyncPathHonorsTimeout() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(200)}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));

            Assert.Throws<ScriptTimeoutException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void T10_StepLimitExceeded() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 10000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T11_ImportedScriptInheritsCancellationToken() {
            ScriptParser innerParser = new();
            IScript imported = innerParser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));

            ScriptParser parser = new() {
                ImportProvider = new FixedImportProvider(new ExternalScriptMethod(imported))
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$m = import(\"whatever\")",
                "$m()"
            ));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(2000)]
        public async Task T12_UsingDoesNotSwallowCancellationOnDisposeFailure() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "using($d) {",
                "  while(true)",
                "    $x = 1",
                "}"
            ));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(new VariableProvider(new Variable("d", new ThrowingDisposable())), cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test, Parallelizable, MaxTime(5000)]
        public void T13a_RegressionNoLimitsCompletesNormally() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$x = 0",
                "for($i=0,$i<1000000,++$i)",
                "  ++$x",
                "$x"
            ));

            Assert.AreEqual(1000000, script.Execute());
        }

        [Test, Parallelizable]
        public void T13b_RegressionRegexDefaultMatchesAsBefore() {
            ScriptParser parser = new();
            IScript script = parser.Parse("\"hello\" ~~ \"^h.*o$\"");

            Assert.AreEqual(true, script.Execute());
        }

        [Test, Parallelizable]
        public void T13c_RegressionUnderStepLimitCompletesNormally() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 1000000}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "$x = 0",
                "for($i=0,$i<100,++$i)",
                "  ++$x",
                "$x"
            ));

            Assert.AreEqual(100, script.Execute());
        }

        [Test, Parallelizable, MaxTime(3000)]
        [Description("DiVoid #7892/#7898: Task.Run(action, ct) skips its delegate entirely when ct is already cancelled while the work item still sits queued, so a blind CancelAfter(200) can cancel before a starved thread pool ever hands the worker a thread — nothing acquired, nothing to unwind, and the old assertion failed on a run that never started. Waits for the engine to actually resolve $d (proof the worker entered the using) before cancelling, so the test asserts unwind ordering rather than pool scheduling latency.")]
        public async Task T14_WorkerUnwindsAndDisposesResourcesOnCancel() {
            ScriptParser parser = new();
            RecordingDisposable disposable = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "using($d) {",
                "  while(true)",
                "    wait(60000)",
                "}"
            ));

            using ManualResetEventSlim resourceResolved = new(false);
            SignalingVariableProvider provider = new(resourceResolved, new Variable("d", disposable));

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(provider, cts.Token);
            Assert.That(resourceResolved.Wait(TimeSpan.FromSeconds(2)), Is.True);
            cts.Cancel();

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
            Assert.That(disposable.Disposed, Is.True);
        }

        [Test, Parallelizable, MaxTime(3000)]
        public async Task T15_TimeoutAndCancelAreDistinguishable() {
            ScriptParser callerParser = new();
            IScript callerScript = callerParser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));
            CancellationTokenSource cts = new();
            Task callerTask = callerScript.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);
            await callerTask.ContinueWith(t => { });
            Assert.That(callerTask.IsCanceled, Is.True);

            ScriptParser timeoutParser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(200)}
            };
            IScript timeoutScript = timeoutParser.Parse(ScriptCode.Create(
                "while(true)",
                "  $x = 1"
            ));
            Task timeoutTask = timeoutScript.ExecuteAsync();
            await timeoutTask.ContinueWith(t => { });
            Assert.That(timeoutTask.IsFaulted, Is.True);
            Assert.That(timeoutTask.Exception?.InnerException, Is.InstanceOf<ScriptTimeoutException>());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7897/#7898: with Timeout also configured, the executing thread's new self-check (ScriptContext.Guard's DeadlineGuard) must not misclassify a genuine caller cancellation as a timeout — GuardedExecution.Convert still keys off the caller's own token, unaffected by the added thread-owned deadline check. Timeout is set far beyond the caller's own cancellation so a regression that always reports ScriptTimeoutException once a deadline is configured cannot pass by accident.")]
        public async Task Timeout_CallerCancelBeforeDeadlineStillThrowsOperationCanceled() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromSeconds(30)}
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
        /// 30 days exceeds the ~24.8 day maximum a single <see cref="WaitHandle.WaitOne(TimeSpan)"/> accepts,
        /// forcing the interruptible wait to chunk internally; it must still cancel promptly rather than
        /// throwing an <see cref="ArgumentOutOfRangeException"/> or blocking for the full duration
        /// </summary>
        [Test, Parallelizable, MaxTime(3000)]
        public async Task Wait_DurationBeyondSingleWaitMaximumIsStillCancellable() {
            ScriptParser parser = new();
            IScript script = parser.Parse("wait(\"30.00:00:00\")");

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync((IVariableProvider)null, cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        /// <summary>
        /// CF-1: <c>AssignableToken.Assign</c> executes the right-hand side inline; a cancellation raised
        /// while evaluating it (here, a guarded <c>count()</c> call over an infinite host sequence) must
        /// reach the caller as a cancelled task, not a wrapped <see cref="ScriptRuntimeException"/>
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public async Task CF1_AssignmentRhsAsyncCancelPropagatesAsCanceled() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$c = $src.count()");

            CancellationTokenSource cts = new();
            Task task = script.ExecuteAsync(new VariableProvider(new Variable("src", InfiniteSequence())), cts.Token);
            cts.CancelAfter(200);

            await task.ContinueWith(t => { });
            Assert.That(task.IsCanceled, Is.True);
        }

        /// <summary>
        /// CF-1 on the sync token path: the same assignment-RHS shape, cancelled via the sync
        /// <c>Execute(vars, ct)</c> overload, must throw <see cref="OperationCanceledException"/> rather
        /// than a wrapped <see cref="ScriptRuntimeException"/>
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void CF1_AssignmentRhsSyncTokenCancelThrowsOperationCanceled() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$c = $src.count()");

            CancellationTokenSource cts = new();
            cts.CancelAfter(200);

            Assert.Throws<OperationCanceledException>(() =>
                script.Execute(new VariableProvider(new Variable("src", InfiniteSequence())), cts.Token));
        }

        /// <summary>
        /// CF-1 under a configured timeout: the assignment-RHS shape must still let a deadline-only
        /// cancellation reach <see cref="GuardedExecution.Convert"/> and surface as <see cref="ScriptTimeoutException"/>,
        /// not a wrapped <see cref="ScriptRuntimeException"/> — this is the case that falsified T15's
        /// Faulted/Canceled split before the fix
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void CF1_AssignmentRhsTimeoutProducesScriptTimeoutException() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(200)}
            };
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$c = $src.count()");

            Assert.Throws<ScriptTimeoutException>(() =>
                script.Execute(new VariableProvider(new Variable("src", InfiniteSequence()))));
        }

        /// <summary>
        /// CF-2: <see cref="ScriptLimits"/> knobs are <c>init</c>-only, so a host configuring one parser's
        /// limits cannot leak that configuration onto another, unconfigured parser sharing
        /// <see cref="ScriptLimits.Default"/> as its default (design §18: was <see cref="ScriptLimits.None"/>
        /// before the secure-by-default flip)
        /// </summary>
        [Test, Parallelizable]
        public void CF2_DefaultLimitsAreNotSharedMutableState() {
            ScriptParser configuredParser = new() {
                Limits = new ScriptLimits {Timeout = TimeSpan.FromMilliseconds(50)}
            };
            ScriptParser defaultParser = new();

            Assert.That(defaultParser.Limits, Is.SameAs(ScriptLimits.Default));
            Assert.That(defaultParser.Limits.Timeout, Is.Null);
            Assert.That(configuredParser.Limits.Timeout, Is.EqualTo(TimeSpan.FromMilliseconds(50)));

            IScript script = defaultParser.Parse(ScriptCode.Create(
                "$x = 0",
                "for($i=0,$i<1000,++$i)",
                "  ++$x",
                "$x"
            ));
            Assert.DoesNotThrow(() => script.Execute());
        }

        /// <summary>
        /// W-2: exercises the <see cref="IScript.Execute{T}(IVariableProvider,CancellationToken)"/> overload
        /// directly — it had zero call sites before this test
        /// </summary>
        [Test, Parallelizable]
        public void SyncTypedExecuteWithToken_ReturnsConvertedResult() {
            ScriptParser parser = new();
            IScript script = parser.Parse("40 + 2");

            int result = script.Execute<int>((IVariableProvider)null, CancellationToken.None);
            Assert.AreEqual(42, result);
        }

        /// <summary>
        /// W-3: exercises <c>Try.cs</c>'s dedicated <see cref="ScriptStepLimitExceededException"/> rethrow —
        /// a step-limit abort must not be swallowed by the script's own <c>catch</c>, unlike an ordinary error
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void Try_DoesNotSwallowStepLimitAbort() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
            };
            IScript script = parser.Parse(ScriptCode.Create(
                "try {",
                "  while(true)",
                "    $x = 1",
                "} catch($e) {",
                "  $caught = true",
                "}"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// W-3: a step-limit abort raised inside a reflected, <see cref="ScriptContext"/>-guarded host call
        /// (<c>EnumerableExtensions.Count</c> here) must reach the caller unwrapped — exercises the
        /// <see cref="ScriptStepLimitExceededException"/> arm of both <c>MethodOperations.CallMethod</c>'s
        /// <see cref="System.Reflection.TargetInvocationException"/> guard and <c>ScriptMethod.ExecuteToken</c>'s
        /// general resolve/call catch chain
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void StepLimit_ThroughReflectedGuardedMethodCallSurfacesUnwrapped() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
            };
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.count()");

            Assert.Throws<ScriptStepLimitExceededException>(() =>
                script.Execute(new VariableProvider(new Variable("src", InfiniteSequence()))));
        }

        /// <summary>
        /// W-3: an imported script whose own parser configures <see cref="ScriptLimits.MaxSteps"/> must let
        /// that step-limit abort reach the outer caller unwrapped — exercises the <see cref="ScriptStepLimitExceededException"/>
        /// arm of <c>ScriptMethod.ExecuteToken</c>'s external-method-invoke catch chain
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void ImportedScript_OwnStepLimitSurfacesUnwrapped() {
            ScriptParser innerParser = new() {
                Limits = new ScriptLimits {MaxSteps = 100}
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
                "$m()"
            ));

            Assert.Throws<ScriptStepLimitExceededException>(() => script.Execute());
        }

        /// <summary>
        /// W-3: an imported script whose own parser configures <see cref="ScriptLimits.Timeout"/> must let
        /// that <see cref="ScriptTimeoutException"/> reach the outer caller unwrapped — exercises the
        /// <see cref="ScriptTimeoutException"/> arm of <c>ScriptMethod.ExecuteToken</c>'s external-method-invoke
        /// catch chain (the only reflection-free path able to produce it, since <c>ScriptTimeoutException</c>
        /// is only ever raised at a <c>Script.Execute</c>/<c>ExecuteAsync</c> boundary, and no reflected
        /// <c>MethodOperations.CallMethod</c> invocation nests one today)
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void ImportedScript_OwnTimeoutSurfacesUnwrapped() {
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
                "$m()"
            ));

            Assert.Throws<ScriptTimeoutException>(() => script.Execute());
        }

        /// <summary>
        /// W-3: T8 only exercised <c>~~</c> under <see cref="ScriptLimits.RegexTimeout"/>; this covers the
        /// symmetric <c>!~</c> operator (<c>MatchesNot</c>)
        /// </summary>
        [Test, Parallelizable, MaxTime(2000)]
        public void MatchesNot_CatastrophicRegexIsBoundedByMatchTimeout() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {RegexTimeout = TimeSpan.FromMilliseconds(100)}
            };
            IScript script = parser.Parse("\"aaaaaaaaaaaaaaaaaaaaaaaaaaaa!\" !~ \"^(a+)+$\"");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() => script.Execute());
            Assert.That(exception.InnerException, Is.InstanceOf<RegexMatchTimeoutException>());
        }
    }
}

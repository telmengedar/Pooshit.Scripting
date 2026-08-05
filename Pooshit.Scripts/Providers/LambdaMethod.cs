using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Providers {

    /// <summary>
    /// lambda method which can get executed
    /// </summary>
    public class LambdaMethod : IExternalMethod {
        readonly string[] parameters;
        readonly ScriptContext context;
        readonly IScriptToken expression;

        /// <summary>
        /// creates a new <see cref="LambdaMethod"/>
        /// </summary>
        /// <param name="parameters">parameters to use for lambda</param>
        /// <param name="context">execution context</param>
        /// <param name="expression">expression to execute</param>
        public LambdaMethod(string[] parameters, ScriptContext context, IScriptToken expression) {
            this.parameters = parameters;
            this.context = context;
            this.expression = expression;
        }

        /// <summary>
        /// invokes the method using this lambda's own captured context for its depth budget
        /// </summary>
        /// <remarks>
        /// The <see cref="ScriptContext.Guard"/> call here is the single highest-leverage checkpoint in the
        /// engine: it covers every path where control is handed back to script code from host-driven
        /// iteration or callbacks and any host-registered extension that invokes a lambda without an
        /// available invoking <see cref="ScriptContext"/> of its own. Prefer <see cref="InvokeFrom"/> wherever
        /// an invoking context is available — this one is a fallback for callers that genuinely have none (eg.
        /// direct C# code outside a script call, like a unit test constructing and invoking a lambda by hand).
        /// A script-level <c>$lambda.invoke(...)</c> call resolves through <see cref="InvokeFrom"/> instead,
        /// via the explicit <see cref="IExternalMethod"/> implementation below — see <see cref="InvokeFrom"/>'s
        /// remarks for why that distinction exists and matters (DiVoid #7749).
        /// <c>task.run</c> bodies go through <see cref="InvokeOnNewStack"/>.
        /// <para>
        /// Deliberately named a distinct method, not a second <c>Invoke</c> overload (DiVoid #7744 round 4):
        /// an earlier version of this fix added <c>public object Invoke(ScriptContext, params object[])</c>
        /// alongside this one, and QA test-compiled the fallout rather than reasoning about it —
        /// <c>lambda.Invoke(null)</c>, ordinary host code invoking a one-parameter lambda with a null
        /// argument, stopped compiling at all (<c>CS0121</c>, ambiguous between the two overloads), and
        /// <c>lambda.Invoke(someContext)</c> silently rebound to the other overload whenever the first
        /// argument's static type happened to be <see cref="ScriptContext"/>. Both are exactly the source
        /// break category worth designing away rather than documenting: retiring the overload removes the
        /// ambiguity structurally, so there is nothing for a future signature change on either method to
        /// collide with again.
        /// </para>
        /// </remarks>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        public object Invoke(params object[] arguments) {
            CheckArguments(arguments);
            context.Guard();
            return InvokeCore(context.DepthBudget, arguments);
        }

        /// <summary>
        /// invokes the method given an explicit invoking context, resolving the depth budget from
        /// <paramref name="invokingContext"/> — the context of the call site invoking this lambda — rather
        /// than from this lambda's own captured context
        /// </summary>
        /// <remarks>
        /// DiVoid #7749 (the CF-1 residual, and its CF-5 follow-up): a lambda captured in one scope and
        /// invoked from inside a <c>task.run</c> body defined in another still needs to be counted against
        /// <em>that body's</em> depth budget, not the budget of whatever context happened to be active when
        /// the lambda was created. The captured context is right for building the lambda's own variable scope
        /// (closures must see the variables visible at definition time) but wrong for depth accounting, which
        /// is about the physical call stack the invocation is actually happening on right now — design §7.1
        /// Error 2 already identified the captured context as the wrong source for depth, for the same
        /// underlying reason.
        /// <para>
        /// Reached two ways. First, <see cref="Tokens.ScriptMethod.ExecuteToken"/> special-cases
        /// <see cref="IExternalMethod"/> hosts to pass the calling <see cref="ScriptContext"/> through
        /// explicitly for a script-level <c>$lambda.invoke(...)</c> call — the same mechanism design §7.3
        /// already uses to cross an <c>import</c> boundary — so the explicit <see cref="IExternalMethod.Invoke"/>
        /// implementation below simply delegates here, reusing existing, explicit, non-ambient plumbing rather
        /// than adding any. Second, and just as load-bearing: host extensions that already receive an invoking
        /// <see cref="ScriptContext"/> via the injection seam (<c>EnumerableExtensions.Where</c>,
        /// <c>.indexof(predicate)</c>, <c>.lastindexof(predicate)</c> — added in #7409) call this method
        /// directly by name. CF-4 found that CF-1's original fix covered only the script-level <c>.invoke()</c>
        /// path and missed these — a predicate lambda captured outside several concurrent <c>task.run</c>
        /// bodies and invoked from inside each of them, entirely without recursion, still breached a shared
        /// budget through <c>.where</c>/<c>.indexof</c>/<c>.lastindexof</c> until those call sites were changed
        /// to call this method too.
        /// </para>
        /// <para>
        /// Deliberately not the <c>[ThreadStatic]</c>/<c>AsyncLocal</c> route design §7.1 rejected for #7713
        /// (the async interpreter rewrite): nothing here is read from thread- or execution-context-local
        /// state, only from a parameter passed down the same explicit call chain every other context-aware
        /// dispatch in the engine already uses, so it survives an eventual async rewrite exactly as the rest
        /// of the context-carried budget design does. As a side effect, a script-level <c>$lambda.invoke()</c>
        /// call no longer goes through reflection at all (<c>ScriptMethod</c>'s <see cref="IExternalMethod"/>
        /// fast path calls this method directly), unlike a lambda reached through a reflected host method.
        /// </para>
        /// <para>
        /// One further consequence worth recording explicitly: after the CF-1/CF-5 fixes, a lambda captured
        /// outside a <c>task.run</c> body and invoked from inside it (whether via <c>.invoke()</c> or via a
        /// host extension calling this method) resolves the <em>task-local</em> budget the invoking
        /// <c>task.run</c> body was given, not the shared root budget it used to resolve before the fix.
        /// <see cref="DepthBudget.CheckBreached"/>'s latch, whose only currently-exercised value (per DiVoid
        /// #7744/#7749 measurement) was exactly a breach landing on that shared root budget while wrapped by
        /// <see cref="System.AggregateException"/>, therefore has no scenario in this test suite that
        /// currently forces it to fire — see its own remarks. It remains in place as insurance against a
        /// still-undiscovered swallow route on a genuinely shared budget.
        /// </para>
        /// </remarks>
        /// <param name="invokingContext">context of the call site invoking this lambda</param>
        /// <param name="arguments">arguments for lambda</param>
        /// <returns>execution result</returns>
        public object InvokeFrom(ScriptContext invokingContext, params object[] arguments) {
            CheckArguments(arguments);
            invokingContext.Guard();
            return InvokeCore(invokingContext.DepthBudget, arguments);
        }

        /// <inheritdoc />
        object IExternalMethod.Invoke(ScriptContext invokingContext, params object[] arguments) => InvokeFrom(invokingContext, arguments);

        /// <summary>
        /// invokes the method as the first frame of a new physical call stack (a <c>task.run</c> body
        /// executing on its own thread pool thread), so a configured recursion-depth ceiling is measured
        /// against just this stack rather than accumulating against the captured context's shared budget
        /// </summary>
        /// <remarks>
        /// DiVoid #7744 CF-1: a <c>task.run</c> body's own <see cref="Invoke"/> call and the caller's already
        /// went through <see cref="ScriptContext.DepthBudget"/>'s <em>same</em> shared counter, because the
        /// budget is propagated by reference for the (correct) reason that recursion through an <c>import</c>
        /// boundary must accumulate on the one shared physical stack (§7.3 of the depth-guard design). A
        /// <c>task.run</c> body is the one construct where that assumption is wrong — it is a genuinely new
        /// physical stack, not a nested frame on the caller's — so counting concurrently-running bodies against
        /// the same counter treats parallelism as nesting: enough concurrent bodies breach a ceiling none of
        /// them individually approaches. A fresh <see cref="DepthBudget"/>, sized to the same
        /// <see cref="Pooshit.Scripting.DepthBudget.Limit"/> but starting at zero, restores "this stack's own
        /// depth" as the thing being measured for <em>this</em> lambda's own invocation. A lambda captured
        /// outside this body and invoked recursively from inside it resolves its own budget separately, from
        /// whatever context is invoking <em>it</em> at that point — see <see cref="InvokeFrom"/>, which is
        /// exactly what closes that case (DiVoid #7749). Kept internal rather than a new public
        /// <see cref="Hosts.TaskHost.Run"/> overload: the reset is purely an invocation-bookkeeping concern
        /// this method already owns end-to-end, not something the host needs to see or configure — unlike
        /// <see cref="Hosts.TaskHost.WaitAll"/>'s <see cref="ScriptContext"/> parameter, which the host
        /// genuinely needs to observe the cancellation token.
        /// </remarks>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        internal object InvokeOnNewStack(params object[] arguments) {
            CheckArguments(arguments);
            context.Guard();
            DepthBudget freshBudget = context.DepthBudget == null ? null : new DepthBudget(context.DepthBudget.Limit);
            return InvokeCore(freshBudget, arguments);
        }

        /// <summary>
        /// validates that <paramref name="arguments"/> matches this lambda's parameter count
        /// </summary>
        /// <remarks>
        /// DiVoid #7744 round 5: <paramref name="arguments"/> can legitimately be <c>null</c> — a bare
        /// <c>null</c> literal passed to <see cref="Invoke"/> (a single-overload <c>params object[]</c>
        /// method) binds in normal form, so the whole array reference is <c>null</c> rather than a
        /// one-element array containing <c>null</c>. Treated as zero arguments rather than dereferenced
        /// directly, so a zero-parameter lambda invoked this way succeeds instead of throwing
        /// <see cref="System.NullReferenceException"/>; a lambda that does take parameters still correctly reports
        /// the arity mismatch, exactly as it would for an empty array. <see cref="InvokeCore"/> never
        /// dereferences <paramref name="arguments"/> by index unless this check already confirmed
        /// <c>parameters.Length</c> elements are present, so a genuinely null array only ever reaches it when
        /// <c>parameters.Length</c> is itself zero — safe by construction, not by coincidence.
        /// </remarks>
        /// <param name="arguments">arguments to validate, or <c>null</c> (treated as zero arguments)</param>
        void CheckArguments(object[] arguments) {
            int argumentCount = arguments?.Length ?? 0;
            if(parameters.Length != argumentCount)
                throw new ScriptRuntimeException($"Argument count doesn't match up parameter count:\n{string.Join(", ", parameters)}", expression);
        }

        /// <summary>
        /// shared body of <see cref="Invoke"/>, <see cref="InvokeFrom"/> and <see cref="InvokeOnNewStack"/>:
        /// this is the single choke point for every lambda call, and therefore the one place that enters a
        /// level of the given <paramref name="depthBudget"/>. Always builds the invocation scope from this
        /// lambda's own captured context — only the depth budget varies by caller
        /// </summary>
        /// <remarks>
        /// <see cref="Pooshit.Scripting.DepthBudget.Enter"/> runs as the first statement inside the
        /// <c>try</c>, so a breach there is still paired with the <c>finally</c>'s
        /// <see cref="Pooshit.Scripting.DepthBudget.Exit"/>, leaving no depth leaked behind even when the
        /// breach itself is what unwinds this call. There is deliberately no physical-stack probe here
        /// (DiVoid #7744 CF-3, measured and dropped): a
        /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack"/> check
        /// was tried and removed after measurement showed it never fires before a real, uncatchable
        /// <see cref="System.StackOverflowException"/> on this engine's reflected recursive call shapes (a
        /// lambda reached through a reflected host method), where many stacked <c>catch (...) when (...)</c>
        /// filter clauses accumulate one per level and are evaluated at full stack depth by .NET's two-pass
        /// exception model before any unwinding happens, consuming whatever margin the probe reserved.
        /// <see cref="ScriptLimits.MaxDepth"/> is therefore the only mechanism that protects the process, and
        /// it must be sized empirically against the actual host build — see
        /// <c>docs/pooscript-language-reference.md</c> §12.
        /// </remarks>
        /// <param name="depthBudget">depth budget to enter for this invocation, or <c>null</c> when unconfigured</param>
        /// <param name="arguments">arguments for lamda</param>
        /// <returns>execution result</returns>
        object InvokeCore(DepthBudget depthBudget, object[] arguments) {
            try {
                depthBudget?.Enter();

                ScriptContext lambdacontext = new ScriptContext(context, depthBudget);
                for(int i = 0; i < parameters.Length; ++i)
                    lambdacontext.Arguments[parameters[i]] = arguments[i];

                return expression.Execute(lambdacontext);
            }
            finally {
                depthBudget?.Exit();
            }
        }
    }
}

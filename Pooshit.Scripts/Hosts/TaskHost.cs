using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pooshit.Scripting.Providers;

namespace Pooshit.Scripting.Hosts {

    /// <summary>
    /// provides methods for tasks
    /// </summary>
    public class TaskHost {

        /// <summary>
        /// starts a new task from a lambda method
        /// </summary>
        /// <remarks>
        /// Runs through <see cref="LambdaMethod.InvokeOnNewStack"/> rather than <see cref="LambdaMethod.Invoke"/>:
        /// this body executes on its own thread pool thread, a genuinely new physical call stack, so it must
        /// not accumulate recursion depth against the caller's already-consumed stack (DiVoid #7744 CF-1).
        /// <paramref name="method"/> itself always gets this fresh, task-local budget. A <em>different</em>
        /// lambda invoked from inside this body — whether captured outside the body or created inside it —
        /// separately resolves its own depth budget from whatever context is invoking <em>it</em> at that
        /// point, which for anything invoked from inside this body is this same task-local one (DiVoid #7749:
        /// see <see cref="LambdaMethod"/>'s <see cref="Data.IExternalMethod"/> implementation). A host does
        /// not need to define recursive lambdas inside the body passed to <see cref="Run"/> for
        /// <c>MaxDepth</c> to bound them correctly.
        /// </remarks>
        /// <param name="method">method to run as task</param>
        /// <returns>task object containing running method</returns>
        public Task<object> Run(LambdaMethod method) {
            return Task.Run(() => method.InvokeOnNewStack());
        }

        /// <summary>
        /// creates a completed task object form a result value
        /// </summary>
        /// <param name="result">value to wrap as task result</param>
        /// <returns>task object</returns>
        public Task<object> FromResult(object result) {
            return Task.FromResult(result);
        }

        /// <summary>
        /// creates a completed task object form a result value
        /// </summary>
        /// <param name="result">value to wrap as task result</param>
        /// <returns>task object</returns>
        public Task<T> FromResult<T>(T result)
        {
            return Task.FromResult(result);
        }

        /// <summary>
        /// waits for all tasks to complete
        /// </summary>
        /// <param name="tasks">tasks to wait for</param>
        /// <param name="context">execution context supplying the cancellation token to observe; injected by the engine, not by the script call</param>
        public void WaitAll(IEnumerable<Task> tasks, ScriptContext context) {
            // the script unwinds on cancellation; the tasks are the host's own and are not cancelled here
            Task.WaitAll(tasks.ToArray(), context.CancellationToken);
        }
    }
}
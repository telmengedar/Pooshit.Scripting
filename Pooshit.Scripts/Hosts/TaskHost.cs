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
        /// <param name="method">method to run as task</param>
        /// <returns>task object containing running method</returns>
        public Task<object> Run(LambdaMethod method) {
            return Task.Run(() => method.Invoke());
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
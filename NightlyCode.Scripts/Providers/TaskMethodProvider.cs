using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NightlyCode.Scripting.Providers {

    /// <summary>
    /// provides methods for tasks
    /// </summary>
    public class TaskMethodProvider {

        /// <summary>
        /// waits for all tasks to complete
        /// </summary>
        /// <param name="tasks">tasks to wait for</param>
        public void WaitAll(IEnumerable<Task> tasks) {
            Task.WaitAll(tasks.ToArray());
        }
    }
}
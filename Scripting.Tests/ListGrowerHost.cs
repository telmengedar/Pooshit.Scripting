using System.Collections.Generic;

namespace Scripting.Tests {

    /// <summary>
    /// host method that doubles a script-held list in host C#, uncharged by every mechanism except the
    /// growth trigger (design docs/architecture/variable-budget-cadence.md §10.2)
    /// </summary>
    public class ListGrowerHost {

        /// <summary>
        /// doubles <paramref name="target"/> by appending a copy of its current contents
        /// </summary>
        /// <param name="target">script-held list to grow</param>
        public void Grow(List<object> target) => target.AddRange(target.ToArray());
    }
}

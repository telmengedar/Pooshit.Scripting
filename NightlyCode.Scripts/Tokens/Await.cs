using System.Threading.Tasks;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// await token used to await async tasks
    /// </summary>
    public class Await : IScriptToken {
        readonly IScriptToken token;

        /// <summary>
        /// creates a new <see cref="Await"/> token
        /// </summary>
        /// <param name="token">token resulting in task to be awaited</param>
        public Await(IScriptToken token) {
            this.token = token;
        }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            object result = token.Execute(context);
            if (!(result is Task task))
                throw new ScriptRuntimeException("Only tasks can get awaited");

            if (task.Status == TaskStatus.Created)
                task.Start();

            task.Wait();
            if (!task.GetType().IsGenericType)
                return null;

            return task.GetType().GetProperty("Result")?.GetValue(task);
        }

        /// <inheritdoc />
        public T Execute<T>(ScriptContext context) {
            return (T) Execute(context);
        }
    }
}
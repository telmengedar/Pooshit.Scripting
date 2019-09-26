using System.Threading;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting {

    /// <summary>
    /// context for script execution
    /// </summary>
    public class ScriptContext {

        /// <summary>
        /// creates a new <see cref="ScriptContext"/>
        /// </summary>
        /// <param name="variables">global script variables</param>
        /// <param name="arguments">arguments provided at runtime</param>
        public ScriptContext(IVariableContext variables, IVariableProvider arguments) {
            Variables = variables;
            Arguments = arguments;
        }

        /// <summary>
        /// creates a new <see cref="ScriptContext"/>
        /// </summary>
        /// <param name="variables">global script variables</param>
        /// <param name="arguments">arguments provided at runtime</param>
        /// <param name="cancellationToken">cancellation token used to abort script execution (optional)</param>
        public ScriptContext(IVariableContext variables, IVariableProvider arguments, CancellationToken cancellationToken) 
        : this(variables, arguments)
        {
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// global script variables
        /// </summary>
        public IVariableContext Variables { get; }

        /// <summary>
        /// arguments provided at runtime
        /// </summary>
        public IVariableProvider Arguments { get; }

        /// <summary>
        /// cancellation token used to abort script execution
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}
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
        /// <param name="context">context to base this context on</param>
        public ScriptContext(ScriptContext context)
        : this(new VariableProvider(context.Arguments), context.TypeProvider, context.CancellationToken) {
        }

        /// <summary>
        /// creates a new <see cref="ScriptContext"/>
        /// </summary>
        /// <param name="arguments">arguments provided at runtime</param>
        /// <param name="typeprovider">access to available types</param>
        public ScriptContext(IVariableProvider arguments, ITypeProvider typeprovider) {
            Arguments = arguments;
            TypeProvider = typeprovider;
        }

        /// <summary>
        /// creates a new <see cref="ScriptContext"/>
        /// </summary>
        /// <param name="arguments">arguments provided at runtime</param>
        /// <param name="typeprovider">access to available types</param>
        /// <param name="cancellationToken">cancellation token used to abort script execution (optional)</param>
        public ScriptContext(IVariableProvider arguments, ITypeProvider typeprovider, CancellationToken cancellationToken)
        : this(arguments, typeprovider) {
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// arguments provided at runtime
        /// </summary>
        public IVariableProvider Arguments { get; }

        /// <summary>
        /// provider for known type information
        /// </summary>
        public ITypeProvider TypeProvider { get; set; }

        /// <summary>
        /// cancellation token used to abort script execution
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}
using System.Reflection;
using Pooshit.Scripting.Operations;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Parser.Resolvers {

    /// <inheritdoc />
    public class ResolvedMethod : IResolvedMethod {
        readonly MethodGuard guard;

        /// <summary>
        /// creates a new <see cref="ResolvedMethod"/>
        /// </summary>
        /// <param name="method">method to call</param>
        /// <param name="referenceParameters">reference parameter information</param>
        /// <param name="isExtension">determines whether invocation is an extension method</param>
        /// <param name="guard">governs which reflected members may be dispatched to, carried through to the invoke-site H2 check</param>
        public ResolvedMethod(MethodInfo method, ReferenceParameter[] referenceParameters, bool isExtension=false, MethodGuard guard=null) {
            Method = method;
            IsExtension = isExtension;
            ReferenceParameters = referenceParameters;
            this.guard = guard;
        }

        /// <summary>
        /// method to call
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// determines whether invocation is an extension method
        /// </summary>
        public bool IsExtension { get; }

        /// <summary>
        /// reference parameter information
        /// </summary>
        public ReferenceParameter[] ReferenceParameters { get; set; }

        /// <inheritdoc />
        public object Call(IScriptToken methodcall, object host, object[] parameters, ScriptContext context) {
            return MethodOperations.CallMethod(methodcall, host, Method, parameters, context, ReferenceParameters, IsExtension, guard);
        }
    }
}
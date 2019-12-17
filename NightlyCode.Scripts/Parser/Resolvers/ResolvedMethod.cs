using System.Reflection;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser.Resolvers {

    /// <inheritdoc />
    public class ResolvedMethod : IResolvedMethod {
        /// <summary>
        /// creates a new <see cref="ResolvedMethod"/>
        /// </summary>
        /// <param name="method">method to call</param>
        /// <param name="referenceParameters">reference parameter information</param>
        /// <param name="isExtension">determines whether invocation is an extension method</param>
        public ResolvedMethod(MethodInfo method, ReferenceParameter[] referenceParameters, bool isExtension=false) {
            Method = method;
            IsExtension = isExtension;
            ReferenceParameters = referenceParameters;
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
            return MethodOperations.CallMethod(methodcall, host, Method, parameters, context, ReferenceParameters, IsExtension);
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// calls a method in a script
    /// </summary>
    class ScriptMethod : IScriptToken {
        readonly IExtensionProvider extensions;
        readonly IScriptToken hosttoken;
        readonly string methodname;
        readonly IScriptToken[] parameters;

        /// <summary>
        /// creates a new <see cref="ScriptMethod"/>
        /// </summary>
        /// <param name="extensions">pool containing known hosts</param>
        /// <param name="hosttoken">host of method to be called</param>
        /// <param name="methodname">name of method to call</param>
        /// <param name="parameters">parameters for method call</param>
        public ScriptMethod(IExtensionProvider extensions, IScriptToken hosttoken, string methodname, IScriptToken[] parameters) {
            this.extensions = extensions;
            this.hosttoken = hosttoken;
            this.methodname = methodname.ToLower();
            this.parameters = parameters;
        }

        /// <inheritdoc />
        public object Execute() {
            object host = hosttoken.Execute();
            if (host == null)
                throw new ScriptExecutionException($"'{hosttoken}' results in null");

            MethodInfo[] methods = host.GetType().GetMethods().Where(m => m.Name.ToLower() == methodname && MethodOperations.MatchesParameterCount(m, parameters)).ToArray();

            StringBuilder executionlog = new StringBuilder();

            foreach(MethodInfo method in methods) {
                try {
                    return MethodOperations.CallMethod(host, method, parameters);
                }
                catch (Exception e) {
                    executionlog.AppendLine(e.Message);
                }
            }

            if (extensions != null) {
                Type extensionbase = host.GetType();
                while (extensionbase != null) {
                    foreach (MethodInfo method in extensions.GetExtensions(extensionbase).Where(m => m.Name.ToLower() == methodname && MethodOperations.MatchesParameterCount(m, parameters, true))) {
                        try {
                            return MethodOperations.CallMethod(host, method, parameters, true);
                        }
                        catch (Exception e) {
                            executionlog.AppendLine(e.Message);
                        }
                    }

                    if (extensionbase == typeof(object))
                        break;
                    extensionbase = extensionbase.BaseType;
                }
            }

            foreach (Type interfacetype in host.GetType().GetInterfaces()) {
                foreach (MethodInfo method in extensions.GetExtensions(interfacetype).Where(m => m.Name.ToLower() == methodname && MethodOperations.MatchesParameterCount(m, parameters, true)))
                {
                    try
                    {
                        return MethodOperations.CallMethod(host, method, parameters, true);
                    }
                    catch (Exception e)
                    {
                        executionlog.AppendLine(e.Message);
                    }
                }
            }

            if (executionlog.Length == 0)
                throw new ScriptRuntimeException($"Method '{methodname}' matching the specified parameters count not found on type {host.GetType().Name}", executionlog.ToString());

            throw new ScriptRuntimeException("None of the matching methods could be invoked using the specified parameters", executionlog.ToString());
        }

        public override string ToString() {
            return $"{hosttoken}.{methodname}({string.Join(",", parameters.Select(p => p.ToString()))})";
        }
    }
}
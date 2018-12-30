using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// calls a method in a script
    /// </summary>
    public class ScriptMethod : IScriptToken {
        readonly IExtensionProvider hostpool;
        readonly IScriptToken hosttoken;
        readonly string methodname;
        readonly IScriptToken[] parameters;

        /// <summary>
        /// creates a new <see cref="ScriptMethod"/>
        /// </summary>
        /// <param name="hostpool">pool containing known hosts</param>
        /// <param name="hosttoken">host of method to be called</param>
        /// <param name="methodname">name of method to call</param>
        /// <param name="parameters">parameters for method call</param>
        public ScriptMethod(IExtensionProvider hostpool, IScriptToken hosttoken, string methodname, IScriptToken[] parameters) {
            this.hostpool = hostpool;
            this.hosttoken = hosttoken;
            this.methodname = methodname.ToLower();
            this.parameters = parameters;
        }

        /// <inheritdoc />
        public object Execute() {
            object host = hosttoken.Execute();
            MethodInfo[] methods = host.GetType().GetMethods().Where(m => m.Name.ToLower() == methodname && m.GetParameters().Length == parameters.Length).ToArray();

            StringBuilder executionlog = new StringBuilder();

            foreach(MethodInfo method in methods) {
                try {
                    return MethodOperations.CallMethod(host, method, parameters);
                }
                catch (Exception e) {
                    executionlog.AppendLine(e.Message);
                }
            }

            Type extensionbase = host.GetType();
            while(extensionbase!=null) {
                foreach (MethodInfo method in hostpool.GetExtensions(extensionbase).Where(m => m.Name.ToLower() == methodname && m.GetParameters().Length == parameters.Length + 1)) {
                    try
                    {
                        return MethodOperations.CallMethod(host, method, parameters, true);
                    }
                    catch (Exception e)
                    {
                        executionlog.AppendLine(e.Message);
                    }
                }

                if (extensionbase == typeof(object))
                    break;
                extensionbase = extensionbase.BaseType;
            }

            foreach (Type interfacetype in host.GetType().GetInterfaces()) {
                foreach (MethodInfo method in hostpool.GetExtensions(interfacetype).Where(m => m.Name.ToLower() == methodname && m.GetParameters().Length == parameters.Length + 1))
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
                throw new ScriptException($"Method '{methodname}' matching the specified parameters count not found on type {host.GetType().Name}", executionlog.ToString());

            throw new ScriptException("None of the matching methods could be invoked using the specified parameters", executionlog.ToString());
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            throw new ScriptException("Assignment to a method is not supported");
        }

        public override string ToString() {
            return $"{hosttoken}.{methodname}({string.Join(",", parameters.Select(p => p.ToString()))})";
        }
    }
}
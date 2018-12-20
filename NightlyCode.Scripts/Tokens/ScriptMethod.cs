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
        readonly IScriptHostPool hostpool;
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
        public ScriptMethod(IScriptHostPool hostpool, IScriptToken hosttoken, string methodname, IScriptToken[] parameters) {
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
                ParameterInfo[] targetparameters = method.GetParameters();
                object[] callparameters;
                try {
                    callparameters = MethodOperations.CreateParameters(targetparameters, parameters).ToArray();
                }
                catch(Exception e) {
                    executionlog.AppendLine($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))}) - {e.Message}");
                    continue;
                }

                try {
                    return method.Invoke(host, callparameters);
                }
                catch (TargetInvocationException e) {
                    executionlog.AppendLine($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)}) - {e.InnerException?.Message??e.Message}");
                }
                catch(Exception e) {
                    executionlog.AppendLine($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)}) - {e.Message}");
                }
            }

            Type extensionbase = host.GetType();
            while(extensionbase!=null) {
                foreach (MethodInfo method in hostpool.GetExtensions(extensionbase).Where(m => m.Name.ToLower() == methodname && m.GetParameters().Length == parameters.Length + 1)) {
                    ParameterInfo[] targetparameters = method.GetParameters();
                    object[] callparameters;
                    try {
                        callparameters = MethodOperations.CreateParameters(host, targetparameters.Skip(1).ToArray(), parameters).ToArray();
                    }
                    catch (Exception e) {
                        executionlog.AppendLine($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))}) - {e.Message}");
                        continue;
                    }

                    try {
                        return method.Invoke(null, callparameters);
                    }
                    catch (TargetInvocationException e)
                    {
                        executionlog.AppendLine($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)}) - {e.InnerException?.Message ?? e.Message}");
                    }
                    catch (Exception e) {
                        executionlog.AppendLine($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)}) - {e.Message}");
                    }
                }

                if (extensionbase == typeof(object))
                    break;
                extensionbase = extensionbase.BaseType;
            }

            foreach (Type interfacetype in host.GetType().GetInterfaces()) {
                foreach (MethodInfo method in hostpool.GetExtensions(interfacetype).Where(m => m.Name.ToLower() == methodname && m.GetParameters().Length == parameters.Length + 1))
                {
                    ParameterInfo[] targetparameters = method.GetParameters();
                    object[] callparameters;
                    try
                    {
                        callparameters = MethodOperations.CreateParameters(host, targetparameters.Skip(1).ToArray(), parameters).ToArray();
                    }
                    catch (Exception e)
                    {
                        executionlog.AppendLine($"Unable to convert parameters for {host.GetType().Name}.{method.Name}({string.Join(",", targetparameters.Select(p => p.ParameterType.Name + " " + p.Name))}) - {e.Message}");
                        continue;
                    }

                    try
                    {
                        return method.Invoke(null, callparameters);
                    }
                    catch (TargetInvocationException e)
                    {
                        executionlog.AppendLine($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)}) - {e.InnerException?.Message ?? e.Message}");
                    }
                    catch (Exception e)
                    {
                        executionlog.AppendLine($"Unable to call {host.GetType().Name}.{method.Name}({string.Join(",", callparameters)}) - {e.Message}");
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
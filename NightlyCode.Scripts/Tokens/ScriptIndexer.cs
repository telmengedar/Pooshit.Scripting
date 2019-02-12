using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// indexer call on an object
    /// </summary>
    class ScriptIndexer : AssignableToken {
        readonly IScriptToken hosttoken;
        readonly IScriptToken[] parameters;

        /// <summary>
        /// creates a new <see cref="ScriptIndexer"/>
        /// </summary>
        /// <param name="hosttoken">token representing host</param>
        /// <param name="parameters">parameters for indexer call</param>
        public ScriptIndexer(IScriptToken hosttoken, IScriptToken[] parameters) {
            this.hosttoken = hosttoken;
            this.parameters = parameters;
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments) {
            object host = hosttoken.Execute(arguments);

            PropertyInfo[] indexer = host.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == parameters.Length).ToArray();
            if (indexer.Length == 0) {
                if (parameters.Length == 1)
                {
                    if (host is Array array)
                        return array.GetValue(Converter.Convert<int>(parameters[0].Execute(arguments)));
                    if (host is IEnumerable enumerable)
                        return enumerable.Cast<object>().Skip(Converter.Convert<int>(parameters[0].Execute(arguments))).First();
                }

                throw new ScriptRuntimeException($"No indexer methods found on {host}");
            }

            object[] parametervalues = parameters.Select(p => p.Execute(arguments)).ToArray();
            Tuple<MethodInfo, int>[] evaluated = indexer.Select(i => MethodOperations.GetMethodMatchValue(i.GetMethod, parametervalues)).Where(e=>e.Item2>=0).ToArray();

            if (evaluated.Length == 0)
                throw new ScriptRuntimeException($"No index getter found on '{host.GetType().Name}' which matched the specified parameters '{string.Join(", ", parametervalues)}'");

            MethodInfo method = evaluated.OrderBy(m => m.Item2).Select(m => m.Item1).First();
            return MethodOperations.CallMethod(host, method, parametervalues);
        }

        protected override object AssignToken(IScriptToken token, IVariableProvider arguments) {
            object host = hosttoken.Execute(arguments);
            if (parameters.Length == 1) {
                if (host is Array array) {
                    object value = token.Execute(arguments);
                    array.SetValue(value, Converter.Convert<int>(parameters[0].Execute(arguments)));
                    return value;
                }
            }

            PropertyInfo[] indexer = host.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == parameters.Length).ToArray();

            object[] parametervalues = parameters.Select(p => p.Execute(arguments)).Concat(new[] {token.Execute(arguments)}).ToArray();
            Tuple<MethodInfo, int>[] evaluated = indexer.Select(i => MethodOperations.GetMethodMatchValue(i.SetMethod, parametervalues)).Where(e=>e.Item2>=0).OrderBy(m=>m.Item2).ToArray();

            if (evaluated.Length == 0)
                throw new ScriptRuntimeException($"No index setter found on '{host.GetType().Name}' which matched the specified parameters '{string.Join(", ", parametervalues)}'");

            return MethodOperations.CallMethod(host, evaluated[0].Item1, parametervalues);
        }

        public override string ToString() {
            return $"{hosttoken}[{string.Join<IScriptToken>(", ", parameters)}]";
        }
    }
}
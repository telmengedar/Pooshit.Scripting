using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// indexer call on an object
    /// </summary>
    class ScriptIndexer : IAssignableToken {
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
        public object Execute() {
            object host = hosttoken.Execute();
            if (parameters.Length == 1) {
                if (host is Array array)
                    return array.GetValue(Converter.Convert<int>(parameters[0].Execute()));
                if (host is IEnumerable enumerable)
                    return enumerable.Cast<object>().Skip(Converter.Convert<int>(parameters[0].Execute())).First();
            }

            PropertyInfo[] indexer = host.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == parameters.Length).ToArray();

            StringBuilder executionlog = new StringBuilder();

            foreach (PropertyInfo method in indexer)
            {
                try
                {
                    return MethodOperations.CallMethod(host, method.GetMethod, parameters,false,method.GetIndexParameters());
                }
                catch (Exception e)
                {
                    executionlog.AppendLine(e.Message);
                }
            }

            if (executionlog.Length == 0)
                throw new ScriptRuntimeException($"Indexer for '{host.GetType().Name}' matching the specified parameters count not found", executionlog.ToString());

            throw new ScriptRuntimeException("None of the matching indexers could be invoked using the specified parameters", executionlog.ToString());

        }

        public object Assign(IScriptToken token) {
            object host = hosttoken.Execute();
            if (parameters.Length == 1) {
                if (host is Array array) {
                    object value = token.Execute();
                    array.SetValue(value, Converter.Convert<int>(parameters[0].Execute()));
                    return value;
                }
            }

            PropertyInfo[] indexer = host.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == parameters.Length).ToArray();

            StringBuilder executionlog = new StringBuilder();

            foreach (PropertyInfo method in indexer)
            {
                try
                {
                    return MethodOperations.CallMethod(host, method.SetMethod, parameters, false, method.GetIndexParameters(), token);
                }
                catch (Exception e)
                {
                    executionlog.AppendLine(e.Message);
                }
            }

            if (executionlog.Length == 0)
                throw new ScriptRuntimeException($"Indexer for '{host.GetType().Name}' matching the specified parameters count not found", executionlog.ToString());

            throw new ScriptRuntimeException("None of the matching indexers could be invoked using the specified parameters", executionlog.ToString());
        }

        public override string ToString() {
            return $"{hosttoken}[{string.Join<IScriptToken>(", ", parameters)}]";
        }
    }
}
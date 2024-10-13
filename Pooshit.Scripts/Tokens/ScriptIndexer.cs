using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Operations;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// indexer call on an object
/// </summary>
public class ScriptIndexer : AssignableToken, IParameterContainer {

    /// <summary>
    /// creates a new <see cref="ScriptIndexer"/>
    /// </summary>
    /// <param name="hosttoken">token representing host</param>
    /// <param name="parameters">parameters for indexer call</param>
    public ScriptIndexer(IScriptToken hosttoken, IScriptToken[] parameters) {
        Host = hosttoken;
        Parameters = parameters;
    }

    /// <summary>
    /// host to call indexer on
    /// </summary>
    public IScriptToken Host { get; }

    /// <inheritdoc />
    IEnumerable<IScriptToken> IParameterContainer.Parameters => Parameters;

    /// <summary>
    /// access to parameters of indexer
    /// </summary>
    public IScriptToken[] Parameters { get; }

    /// <inheritdoc />
    public bool ParametersOptional => false;

    /// <inheritdoc />
    public override string Literal => "[]";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        object host = Host.Execute(context);

        PropertyInfo[] indexer = host.GetType().GetProperties().Where(p => {
                                                                          ParameterInfo[] parameters = p.GetIndexParameters();
                                                                          if(parameters.Length == 0)
                                                                              return false;

                                                                          if(Attribute.IsDefined(parameters.Last(), typeof(ParamArrayAttribute)))
                                                                              return Parameters.Length >= parameters.Length - 1;
                                                                          return parameters.Length == Parameters.Length;
                                                                      }).ToArray();

        if(indexer.Length == 0) {
            if(Parameters.Length == 1) {
                if(host is Array array)
                    return array.GetValue(Converter.Convert<int>(Parameters[0].Execute(context)));
                if(host is IEnumerable enumerable)
                    return enumerable.Cast<object>().Skip(Converter.Convert<int>(Parameters[0].Execute(context))).First();
            }

            throw new ScriptRuntimeException($"No indexer methods found on {host}", this);
        }

        object[] parametervalues = Parameters.Select(p => p.Execute(context)).ToArray();
        Tuple<MethodInfo, int>[] evaluated = indexer.Select(i => new Tuple<MethodInfo, int>(i.GetMethod, MethodOperations.GetMethodMatchValue(i.GetMethod, parametervalues))).Where(e => e.Item2 >= 0).ToArray();

        if(evaluated.Length == 0)
            throw new ScriptRuntimeException($"No index getter found on '{host.GetType().Name}' which matched the specified parameters '{string.Join(", ", parametervalues)}'", this);

        MethodInfo method = evaluated.OrderBy(m => m.Item2).Select(m => m.Item1).First();
        return MethodOperations.CallMethod(this, host, method, parametervalues, context);
    }

    /// <inheritdoc />
    protected override object AssignToken(IScriptToken token, ScriptContext context) {
        object host = Host.Execute(context);
        if(Parameters.Length == 1) {
            if(host is Array array) {
                object value = token.Execute(context);
                array.SetValue(value, Converter.Convert<int>(Parameters[0].Execute(context)));
                return value;
            }
        }

        PropertyInfo[] indexer = host.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == Parameters.Length).ToArray();

        object[] parametervalues = Parameters.Select(p => p.Execute(context)).Concat(new[] { token.Execute(context) }).ToArray();
        Tuple<MethodInfo, int>[] evaluated = indexer.Select(i => new Tuple<MethodInfo, int>(i.SetMethod, MethodOperations.GetMethodMatchValue(i.SetMethod, parametervalues))).Where(e => e.Item2 >= 0).OrderBy(m => m.Item2).ToArray();

        if(evaluated.Length == 0)
            throw new ScriptRuntimeException($"No index setter found on '{host.GetType().Name}' which matched the specified parameters '{string.Join(", ", parametervalues)}'", this);

        return MethodOperations.CallMethod(this, host, evaluated[0].Item1, parametervalues, context);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{Host}[{string.Join<IScriptToken>(", ", Parameters)}]";
    }
}
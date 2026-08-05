using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting;

/// <summary>
/// script parsed by <see cref="ScriptParser"/>
/// </summary>
class Script : IScript {
    readonly ITypeProvider typeprovider;
    readonly IScriptToken script;
    readonly ScriptLimits limits;

    /// <summary>
    /// creates a new <see cref="Script"/>
    /// </summary>
    /// <param name="script">root token of script to be executed</param>
    /// <param name="typeprovider">access to installed types</param>
    /// <param name="limits">execution guards captured from the parser at parse time</param>
    internal Script(IScriptToken script, ITypeProvider typeprovider, ScriptLimits limits) {
        this.script = script;
        this.typeprovider = typeprovider;
        this.limits = limits ?? ScriptLimits.None;
    }

    T ConvertResult<T>(object result) {
        if(result is T execute)
            return execute;

        if (result is IDictionary dictionary)
            return dictionary.ToType<T>();
            
        try {
            return Converter.Convert<T>(result);
        }
        catch(Exception e) {
            throw new ScriptRuntimeException($"Unable to convert return value of script to {nameof(T)}", null, e);
        }
    }

    /// <inheritdoc />
    public Task<T> ExecuteAsync<T>(IDictionary<string, object> variables, CancellationToken cancellationtoken = default) {
        return ExecuteAsync<T>(new VariableProvider(variables), cancellationtoken);
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(IVariableProvider variables = null, CancellationToken cancellationtoken = default) {
        object result = await ExecuteAsync(variables, cancellationtoken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// script body
    /// </summary>
    public IScriptToken Body => script;

    /// <inheritdoc />
    public object Execute(IDictionary<string, object> variables) {
        return Execute(new VariableProvider(variables));
    }

    /// <inheritdoc />
    public object Execute(IVariableProvider variables = null) {
        return Execute(variables, CancellationToken.None);
    }

    /// <inheritdoc />
    public object Execute(IVariableProvider variables, CancellationToken cancellationToken) {
        using GuardedExecution execution = GuardedExecution.Prepare(variables, typeprovider, cancellationToken, limits);
        try {
            return script.Execute(execution.Context);
        }
        catch (OperationCanceledException e) {
            return execution.Convert(e);
        }
    }

    /// <inheritdoc />
    public T Execute<T>(IDictionary<string, object> variables) {
        return Execute<T>(new VariableProvider(variables));
    }

    /// <inheritdoc />
    public T Execute<T>(IVariableProvider variables, CancellationToken cancellationToken) {
        object result = Execute(variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <inheritdoc />
    public Task<object> ExecuteAsync(IDictionary<string, object> variables, CancellationToken cancellationtoken = default) {
        return ExecuteAsync(new VariableProvider(variables), cancellationtoken);
    }

    /// <inheritdoc />
    public async Task<object> ExecuteAsync(IVariableProvider variables = null, CancellationToken cancellationtoken = default) {
        using GuardedExecution execution = GuardedExecution.Prepare(variables, typeprovider, cancellationtoken, limits);
        try {
            return await Task.Run(() => script.Execute(execution.Context), execution.ExecutionToken);
        }
        catch (OperationCanceledException e) {
            return execution.Convert(e);
        }
    }

    /// <inheritdoc />
    public T Execute<T>(IVariableProvider variables = null) {
        object result = Execute(variables);
        return ConvertResult<T>(result);
    }
}
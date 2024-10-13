using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Tokens;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser.Resolvers;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// calls a method in a script
/// </summary>
public class ScriptMethod : ScriptToken, IParameterContainer {
    readonly IMethodResolver resolver;

    /// <summary>
    /// creates a new <see cref="ScriptMethod"/>
    /// </summary>
    /// <param name="resolver">resolves method calls</param>
    /// <param name="hosttoken">host of method to be called</param>
    /// <param name="methodname">name of method to call</param>
    /// <param name="parameters">parameters for method call</param>
    /// <param name="genericparameters">generic parameters used for generic method templates</param>
    public ScriptMethod(IMethodResolver resolver, IScriptToken hosttoken, string methodname, IScriptToken[] parameters, IScriptToken[] genericparameters=null) {
        Host = hosttoken;
        MethodName = methodname.ToLower();
        Parameters = parameters;
        GenericParameters = genericparameters;
        this.resolver = resolver;
    }

    /// <summary>
    /// host to call method on
    /// </summary>
    public IScriptToken Host { get; }

    /// <summary>
    /// name of method to call
    /// </summary>
    public string MethodName { get; }

    /// <inheritdoc />
    IEnumerable<IScriptToken> IParameterContainer.Parameters => Parameters;

    /// <summary>
    /// direct access to parameters for this method call
    /// </summary>
    public IScriptToken[] Parameters { get; }

    /// <summary>
    /// generic parameters for generic method templates
    /// </summary>
    public IScriptToken[] GenericParameters { get; }

    /// <inheritdoc />
    public bool ParametersOptional => false;

    /// <inheritdoc />
    public override string Literal => $".{MethodName}()";

    Type CreateGenericParameters(ScriptContext context, IScriptToken token) {
        object value = token.Execute(context);
        if (value is Type type)
            return type;

        string typename = value.ToString();
        type = context.TypeProvider.GetType(typename)?.ProvidedType;
        return type;
    }

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        object host = Host.Execute(context);
        if(host == null)
            throw new ScriptRuntimeException($"'{Host}' results in null", this);

        if(host is IExternalMethod externmethod && MethodName.ToLower() == "invoke") {
            try {
                return externmethod.Invoke(context.Arguments, Parameters.Select(a => a.Execute(context)).ToArray());
            }
            catch(Exception e) {
                throw new ScriptRuntimeException($"Error calling external method '{externmethod}'", this, e);
            }
        }

        object[] parametervalues = Parameters.Select(p => p.Execute(context)).ToArray();
        List<ReferenceParameter> references = [];
        for(int i = 0; i < Parameters.Length; ++i) {
            if(Parameters[i] is not Reference r)
                continue;
            references.Add(new(i, r));
        }

        Type[] genericparameters = null;
        if (GenericParameters != null)
            genericparameters = GenericParameters.Select(p => CreateGenericParameters(context, p)).ToArray();

        try {
            IResolvedMethod method = resolver.Resolve(host, MethodName, parametervalues, references.ToArray(), genericparameters);
            return method.Call(this, host, parametervalues, context);
        }
        catch(ScriptRuntimeException e) {
            throw new ScriptRuntimeException(e.Message, this, e.InnerException);
        }
        catch(Exception e) {
            throw new ScriptRuntimeException($"Unable to call {host.GetType().Name}.{MethodName}({string.Join(", ", parametervalues)})", this, e);
        }
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{Host}.{MethodName}({string.Join(",", Parameters.Select(p => p.ToString()))})";
    }
}
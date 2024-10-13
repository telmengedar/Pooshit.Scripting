using System.Collections.Generic;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// control token which is based on a condition
/// </summary>
public interface IParameterContainer : IScriptToken {

    /// <summary>
    /// evaluated condition
    /// </summary>
    IEnumerable<IScriptToken> Parameters { get; }

    /// <summary>
    /// determines whether specification of parameters is optional
    /// </summary>
    bool ParametersOptional { get; }
}
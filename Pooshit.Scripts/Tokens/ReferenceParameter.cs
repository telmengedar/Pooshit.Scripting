using Pooshit.Scripting.Operations;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// parameter to use in a reference call
/// </summary>
public class ReferenceParameter {

    /// <summary>
    /// creates a new <see cref="ReferenceParameter"/>
    /// </summary>
    /// <param name="index">index of parameter</param>
    /// <param name="variable">variable to write parameter value to after call</param>
    public ReferenceParameter(int index, IAssignableToken variable) {
        Index = index;
        Variable = variable;
    }

    /// <summary>
    /// index of parameter
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// variable to write parameter value to after call
    /// </summary>
    public IAssignableToken Variable { get; }
}
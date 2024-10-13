namespace Pooshit.Scripting.Visitors;

/// <summary>
/// info about a parameter in script
/// </summary>
public class ParameterInfo {

    /// <summary>
    /// creates a new <see cref="ParameterInfo"/>
    /// </summary>
    /// <param name="name">name of paramter</param>
    /// <param name="isOptional">determines whether parameter needs to be specified for script to work</param>
    public ParameterInfo(string name, bool isOptional) {
        Name = name;
        IsOptional = isOptional;
    }

    /// <summary>
    /// name of parameter
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// determines whether the parameter has a default value
    /// </summary>
    public bool IsOptional { get; }
}
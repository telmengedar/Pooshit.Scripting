
namespace Pooshit.Scripting.Tokens;

/// <summary>
/// token of a script which can get executed
/// </summary>
public interface IScriptToken {

    /// <summary>
    /// literal which identify the token
    /// </summary>
    string Literal { get; }

    /// <summary>
    /// executes the token returning a result
    /// </summary>
    /// <returns>result of token call</returns>
    object Execute(ScriptContext context);
}
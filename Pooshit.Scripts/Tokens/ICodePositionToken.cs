namespace Pooshit.Scripting.Tokens;

/// <summary>
/// token which contains information about its position in original source code
/// </summary>
public interface ICodePositionToken : IScriptToken {

    /// <summary>
    /// line number where token is stored
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// index in text where token starts
    /// </summary>
    int TextIndex { get; }

    /// <summary>
    /// length of token in code
    /// </summary>
    int TokenLength { get; }
}
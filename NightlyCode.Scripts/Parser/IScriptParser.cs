using System.Threading.Tasks;
using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// interface for a parser of script code
    /// </summary>
    public interface IScriptParser {

        /// <summary>
        /// access to extensions available to script environment
        /// </summary>
        IExtensionProvider Extensions { get; }

        /// <summary>
        /// access to types which can be created using 'new' keyword
        /// </summary>
        ITypeProvider Types { get; }

        /// <summary>
        /// variables available to script
        /// </summary>
        IVariableProvider GlobalVariables { get; }

        /// <summary>
        /// parses a script for execution
        /// </summary>
        /// <param name="data">data to parse</param>
        /// <param name="variables">variables valid for this script (flagged as read-only)</param>
        /// <returns>script which can get executed</returns>
        IScript Parse(string data, params Variable[] variables);

        /// <summary>
        /// parses a script for execution in a task
        /// </summary>
        /// <param name="data">script code to parse</param>
        /// <param name="variables">variables valid for this script (flagged as read-only)</param>
        /// <returns>script parsed from code</returns>
        Task<IScript> ParseAsync(string data, params Variable[] variables);
    }
}
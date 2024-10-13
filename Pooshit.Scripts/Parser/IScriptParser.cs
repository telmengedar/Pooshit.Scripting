﻿using System.Threading.Tasks;
using NightlyCode.Scripting.Providers;

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
        /// resolver which is used by 'import' statement to import methods
        /// </summary>
        IImportProvider ImportProvider { get; set; }

        /// <summary>
        /// parses a script for execution
        /// </summary>
        /// <param name="data">data to parse</param>
        /// <returns>script which can get executed</returns>
        IScript Parse(string data);

        /// <summary>
        /// parses a script for execution in a task
        /// </summary>
        /// <param name="data">script code to parse</param>
        /// <returns>script parsed from code</returns>
        Task<IScript> ParseAsync(string data);
    }
}
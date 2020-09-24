using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// interface for a script
    /// </summary>
    /// <remarks>does the same as <see cref="IScriptToken"/> but has a more clear name</remarks>
    public interface IScript {

        /// <summary>
        /// executes the script and returns the result
        /// </summary>
        /// <returns>script result</returns>
        object Execute(IDictionary<string, object> variables);

        /// <summary>
        /// executes the script and returns the result
        /// </summary>
        /// <returns>script result</returns>
        object Execute(IVariableProvider variables = null);

        /// <summary>
        /// executes the script and returns a typed result
        /// </summary>
        /// <remarks>
        /// this just executes <see cref="Execute(IDictionary{string,object})"/> and tries to convert the result
        /// </remarks>
        /// <typeparam name="T">type of result to return</typeparam>
        /// <returns>result of script execution</returns>
        T Execute<T>(IDictionary<string, object> variables);

        /// <summary>
        /// executes the script and returns a typed result
        /// </summary>
        /// <remarks>
        /// this just executes <see cref="Execute(IDictionary{string,object})"/> and tries to convert the result
        /// </remarks>
        /// <typeparam name="T">type of result to return</typeparam>
        /// <returns>result of script execution</returns>
        T Execute<T>(IVariableProvider variables = null);

        /// <summary>
        /// executes the script and returns the result
        /// </summary>
        /// <returns>script result</returns>
        Task<object> ExecuteAsync(IDictionary<string, object> variables, CancellationToken cancellationtoken = default);

        /// <summary>
        /// executes the script and returns the result
        /// </summary>
        /// <returns>script result</returns>
        Task<object> ExecuteAsync(IVariableProvider variables = null, CancellationToken cancellationtoken = default);

        /// <summary>
        /// executes the script and returns a typed result
        /// </summary>
        /// <remarks>
        /// this just executes <see cref="Execute(IDictionary{string,object})"/> and tries to convert the result
        /// </remarks>
        /// <typeparam name="T">type of result to return</typeparam>
        /// <returns>result of script execution</returns>
        Task<T> ExecuteAsync<T>(IDictionary<string, object> variables, CancellationToken cancellationtoken = default);

        /// <summary>
        /// executes the script and returns a typed result
        /// </summary>
        /// <remarks>
        /// this just executes <see cref="Execute(IDictionary{string,object})"/> and tries to convert the result
        /// </remarks>
        /// <typeparam name="T">type of result to return</typeparam>
        /// <returns>result of script execution</returns>
        Task<T> ExecuteAsync<T>(IVariableProvider variables = null, CancellationToken cancellationtoken = default);

        /// <summary>
        /// script body
        /// </summary>
        IScriptToken Body { get; }
    }
}
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Scripting.Data;
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
        object Execute(params Variable[] variables);

        /// <summary>
        /// executes the script and returns the result
        /// </summary>
        /// <returns>script result</returns>
        Task<object> ExecuteAsync(CancellationToken cancellationtoken, params Variable[] variables);

        /// <summary>
        /// executes the script and returns a typed result
        /// </summary>
        /// <remarks>
        /// this just executes <see cref="Execute"/> and tries to convert the result
        /// </remarks>
        /// <typeparam name="T">type of result to return</typeparam>
        /// <returns>result of script execution</returns>
        T Execute<T>(params Variable[] variables);

        /// <summary>
        /// executes the script and returns a typed result
        /// </summary>
        /// <remarks>
        /// this just executes <see cref="Execute"/> and tries to convert the result
        /// </remarks>
        /// <typeparam name="T">type of result to return</typeparam>
        /// <returns>result of script execution</returns>
        Task<T> ExecuteAsync<T>(CancellationToken cancellationtoken, params Variable[] variables);

        /// <summary>
        /// script body
        /// </summary>
        IScriptToken Body { get; }
    }
}
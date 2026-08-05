namespace Pooshit.Scripting.Data {

    /// <summary>
    /// interface for an external method which can get called from a script
    /// </summary>
    public interface IExternalMethod {

        /// <summary>
        /// invokes the method with the specified arguments
        /// </summary>
        /// <param name="context">execution context of the calling script; carries its cancellation token and arguments (<see cref="ScriptContext.Arguments"/> is the calling script's variable provider)</param>
        /// <param name="arguments">arguments for script method</param>
        /// <returns>result of script execution</returns>
        object Invoke(ScriptContext context, params object[] arguments);
    }
}

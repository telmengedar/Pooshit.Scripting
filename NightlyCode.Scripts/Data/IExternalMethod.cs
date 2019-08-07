namespace NightlyCode.Scripting.Data {

    /// <summary>
    /// interface for an external method which can get called from a script
    /// </summary>
    public interface IExternalMethod {

        /// <summary>
        /// invokes the method with the specified arguments
        /// </summary>
        /// <param name="arguments">arguments for script method</param>
        /// <returns>result of script execution</returns>
        object Invoke(params object[] arguments);
    }
}
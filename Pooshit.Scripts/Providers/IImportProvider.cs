namespace Pooshit.Scripting.Providers {

    /// <summary>
    /// interface for an external method provider
    /// </summary>
    public interface IImportProvider {

        /// <summary>
        /// imports a method or type using the specified argument
        /// </summary>
        /// <param name="parameters">parameters for import</param>
        /// <returns>type loaded from parameters</returns>
        object Import(object[] parameters);
    }
}
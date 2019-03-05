using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Providers {

    /// <summary>
    /// interface for an external method provider
    /// </summary>
    public interface IExternalMethodProvider {

        /// <summary>
        /// imports a script method using the specified argument
        /// </summary>
        /// <param name="key">key used to identify the method to import</param>
        /// <returns>loaded from key</returns>
        IExternalMethod Import(string key);
    }
}
using System.IO;
using System.Reflection;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Providers {

    /// <summary>
    /// provides external methods from a file path
    /// </summary>
    public class FileMethodProvider : IExternalMethodProvider {
        readonly IScriptParser scriptparser;

        /// <summary>
        /// creates a new <see cref="FileMethodProvider"/>
        /// </summary>
        /// <param name="scriptparser">parser used to parse and compile script code</param>
        public FileMethodProvider(IScriptParser scriptparser) {
            this.scriptparser = scriptparser;
        }

        /// <summary>
        /// loads a script from a file to provide an external method
        /// </summary>
        /// <param name="key">path to scriptfile to load and compile</param>
        /// <returns>compiled script as a external method</returns>
        public IExternalMethod Import(string key) {
            string fullpath = Path.Combine(Path.GetDirectoryName((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location), key);
            if (!File.Exists(fullpath))
                throw new FileNotFoundException("External script not found", fullpath);

            return new ExternalScriptMethod(scriptparser.Parse(File.ReadAllText(fullpath)));
        }
    }
}
using System.IO;
using System.Reflection;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting.Providers {

    /// <summary>
    /// provides script methods from assembly resources
    /// </summary>
    public class ResourceScriptMethodProvider : IImportProvider {
        readonly Assembly assembly;
        readonly IScriptParser parser;

        /// <summary>
        /// creates a new <see cref="ResourceScriptMethodProvider"/>
        /// </summary>
        /// <param name="assembly">assembly from which to get script resources</param>
        /// <param name="parser">parser used to parse and compile scripts</param>
        public ResourceScriptMethodProvider(Assembly assembly, IScriptParser parser) {
            this.assembly = assembly;
            this.parser = parser;
        }

        /// <summary>
        /// imports an external script method assembly resources
        /// </summary>
        /// <param name="parameters">resource name</param>
        /// <returns>script method stored in resource</returns>
        public object Import(object[] parameters) {
            if (parameters.Length == 0)
                throw new ScriptRuntimeException("A resource to import is necessary", null);
            if (parameters.Length > 1)
                throw new ScriptRuntimeException("Too many arguments provided. Only a resource path is necessary.", null);

            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(parameters[0].ToString())))
                return new ExternalScriptMethod(parser.Parse(reader.ReadToEnd()));
        }
    }
}
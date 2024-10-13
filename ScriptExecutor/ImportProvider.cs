using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Pooshit.Scripting.Providers;

namespace NightlyCode.ScriptExecutor {
    public class ImportProvider : IImportProvider {

        static ImportProvider() {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                if (string.IsNullOrEmpty(args.Name))
                    return null;

                AssemblyName name = new AssemblyName(args.Name);
                if (File.Exists(name.Name + ".dll"))
                    return Assembly.LoadFrom(name.Name + ".dll");
                return null;
            };
        }
        public ImportProvider() {
        }

        public object Import(object[] parameters) {
            if(parameters.Length<2)
                throw new ArgumentException("import needs 2 arguments");

            if (parameters[0] is string context) {
                switch (context.ToLower()) {
                case "assembly":
                    return Assembly.LoadFile(Path.GetFullPath(parameters[1]?.ToString()));
                case "type":
                    return Type.GetType(parameters[1]?.ToString());
                default:
                    throw new ArgumentException($"Unsupported import type '{parameters[0]}'");
                }
            }

            if (parameters[0] is Assembly assembly) {
                Type type = assembly.GetType(parameters[1]?.ToString(), true);
                if (type == null)
                    throw new ArgumentException($"type '{parameters[1]}' not found in '{assembly.FullName}'");
                return Activator.CreateInstance(type, parameters.Skip(2).ToArray());
            }

            throw new ArgumentException($"Unsupported import type '{parameters[0]}'");
        }
    }
}
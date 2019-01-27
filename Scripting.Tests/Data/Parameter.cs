using System.Reflection.Metadata.Ecma335;

namespace Scripting.Tests.Data {

    public class Parameter : IParameter {
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString() {
            return $"{Name}={Value}";
        }
    }
}
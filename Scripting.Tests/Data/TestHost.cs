using System.Collections.Generic;

namespace Scripting.Tests.Data {
    public class TestHost {
        readonly Dictionary<string, object> values=new Dictionary<string, object>();

        public object this[string key] {
            get => values[key];
            set => values[key] = value;
        }
        
        public string TestMethod(string parameter, string[] parameters)
        {
            return $"{parameter}_{string.Join(",", parameters)}";
        }

        public void AddTestHost(string key, TestHost host) {
            values[key] = host;
        }
    }
}
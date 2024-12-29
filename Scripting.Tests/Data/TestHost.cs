using System.Collections.Generic;

namespace Scripting.Tests.Data;

public class TestHost {
    readonly Dictionary<string, object> values=new();

    public object this[string key] {
        get => values[key];
        set => values[key] = value;
    }

    public int Property { get; set; }

    public int Integer(int data) {
        return data;
    }

    public float Float(float data) {
        return data;
    }

    public string TestMethod(string parameter, string[] parameters)
    {
        return $"{parameter}_{string.Join(",", parameters)}";
    }

    public void AddTestHost(string key, TestHost host) {
        values[key] = host;
    }
}
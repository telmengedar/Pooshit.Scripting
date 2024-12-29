namespace Scripting.Tests.Data;

public class TestExtensions {

    public static string Append(string original, string appendix) {
        return original + appendix;
    }

    public static string Append<T>(T original, T appendix) {
        return appendix.ToString() + original;
    }

    public static int Hashy(object @object) {
        return @object.GetHashCode();
    }

    public static int Optional(int a, int b = 2, int c = 8) => a + b + c;
}
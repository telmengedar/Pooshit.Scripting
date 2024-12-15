namespace Scripting.Tests.Data;

public class TestExtensions {

    public static string Append(string original, string appendix) {
        return original + appendix;
    }

    public static int Hashy(object @object) {
        return @object.GetHashCode();
    }
}
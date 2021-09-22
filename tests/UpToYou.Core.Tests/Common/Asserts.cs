using KellermanSoftware.CompareNetObjects;

namespace UpToYou.Core.Tests {
public static class 
    Asserts {
    public static CompareLogic 
    DefaultCompareLogic = new();
    
    public static void
    DeepAssert<T>(this T actual, T expected, CompareLogic compareLogic = null, string message = null) {
        var result= (compareLogic ?? DefaultCompareLogic).Compare(expected, actual);
        if (!result.AreEqual)
            NUnit.Framework.Assert.Fail(message != null ? message + "\n" + result.DifferencesString : result.DifferencesString);
    }

    public static void
    AssertTrue(this bool actual, string? message = null) => actual.Assert(true, message);

    public static void
    AssertFalse(this bool actual, string? message = null) => actual.Assert(false, message);
    
    public static void
    Assert(this object actual, object expected, string message = null) =>
        NUnit.Framework.Assert.AreEqual(expected, actual, message);
}
}
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Dummy class required for C# 9 record support when targeting
    /// older frameworks (like the one Unity ships).
    /// The compiler looks for this type when emitting init-only
    /// setters, so providing it avoids CS0518 errors.
    /// </summary>
    internal static class IsExternalInit { }
}

#if !NET5_0_OR_GREATER
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for the C# 9 <c>init</c>-only setter compiler contract on
/// target frameworks (like netstandard2.1) that don't ship <c>IsExternalInit</c>
/// in the base class library.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
#endif

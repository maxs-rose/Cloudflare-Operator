using System.Collections.Immutable;

namespace CloudflareOperator.Extensions;

public static class ArrayExtensions
{
    public static void Deconstruct<T>(this ImmutableArray<T> array, out T first, out ImmutableArray<T> rest)
    {
        first = array[0];
        rest = array[1..];
    }
}
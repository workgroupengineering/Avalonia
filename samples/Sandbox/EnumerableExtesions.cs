using System;
using System.Collections.Generic;

namespace Sandbox
{
    static internal class EnumerableExtesions
    {
        public static IEnumerable<TResult> Select<TSource, TArg, TResult>(this IEnumerable<TSource> source, Func<TSource, TArg, TResult> selector, TArg arg)
        {
            if (source == null)
                yield break;
            var enumerator = source?.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return selector(enumerator.Current, arg);
            }
        }
    }
}

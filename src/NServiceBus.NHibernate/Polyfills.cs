// polyfill for missing System.Data.Extensions in .NET Standard/Core https://github.com/dotnet/corefx/issues/19771

#if NETSTANDARD2_0
namespace System.Data
{
    using Collections.Generic;
    using Linq;

    static class Polyfills
    {
        public static IEnumerable<DataRow> AsEnumerable(this DataTable source) => source.Rows.OfType<DataRow>();
    }
}
#endif
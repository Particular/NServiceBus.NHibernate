namespace System.Data
{
    using Collections.Generic;
    using Linq;

    static class DataSetExtensions
    {
        public static IEnumerable<DataRow> AsEnumerable(this DataTable source) => source.Rows.OfType<DataRow>();
    }
}

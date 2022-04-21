using System.Data;

namespace Postgres.SchemaUpdater
{
    public static class ExtensionMethods
    {
        public static IList<T> ToList<T>(this IDataReader reader, Func<IDataRecord, T> generator)
        {
            var list = new List<T>();

            while (reader.Read())
            {
                list.Add(generator(reader));
            }

            return list;
        }
    }
}

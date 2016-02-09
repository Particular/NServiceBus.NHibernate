namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;

    static class ObjectSerializer
    {
        public static string Serialize<T>(T instance)
        {
            var serializer = BuildSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, instance);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static T DeSerialize<T>(string data)
        {
            var serializer = BuildSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        static DataContractJsonSerializer BuildSerializer(Type objectType)
        {
            var settings = new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            };
            return new DataContractJsonSerializer(objectType, settings);
        }
    }
}
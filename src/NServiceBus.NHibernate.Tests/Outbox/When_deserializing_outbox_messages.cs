namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Persistence.NHibernate;
    using NUnit.Framework;

    [TestFixture]
    public class When_serializing_outbox_messages
    {
        static readonly Dictionary<string, string> Expected = new Dictionary<string, string>
        {
            { "SimpleKey", "SimpleValue"},
            { "lowercaseKey", "lowercaseValue"},
            { "qote\"ke'y", "quote\"valu'e"},
            { "paren}key{}", "parent}value{}"},
        };

        [Test]
        public void Should_deserialized_header_be_the_same()
        {
            var headersDeserialized = ObjectSerializer.DeSerialize<Dictionary<string, string>>(ObjectSerializer.Serialize(Expected));

            CollectionAssert.AreEquivalent(Expected, headersDeserialized);
        }

        [Test]
        public void Test_compatibility_with_Newtonsoft()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Auto,
                Converters =
                {
                    new IsoDateTimeConverter
                    {
                        DateTimeStyles = DateTimeStyles.RoundtripKind
                    }
                }
            };

            var headersDeserialized = ObjectSerializer.DeSerialize<Dictionary<string, string>>(JsonConvert.SerializeObject(Expected, Formatting.None, serializerSettings));
            CollectionAssert.AreEquivalent(Expected, headersDeserialized);
        }
    }
}
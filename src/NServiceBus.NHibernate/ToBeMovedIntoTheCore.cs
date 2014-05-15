namespace NServiceBus
{
    using System;
    using System.Linq;

    public static class PersistenceConfig
    {
        public static Configure UsePersistence<T>(this Configure config, Action<PersistenceConfiguration> customizations = null) where T : PersistenceDefinition
        {
            return UsePersistence(config, typeof(T), customizations);
        }

        public static Configure UsePersistence(this Configure config, Type definitionType, Action<PersistenceConfiguration> customizations = null)
        {
            var type =
              Configure.TypesToScan.SingleOrDefault(
                  t => typeof(IConfigurePersistence<>).MakeGenericType(definitionType).IsAssignableFrom(t));

            if (type == null)
                throw new InvalidOperationException(
                    "We couldn't find a IConfigureTransport implementation for your selected transport: " +
                    definitionType.Name);


            var c = new PersistenceConfiguration();

            if (customizations != null)
            {
                customizations(c);
            }

            ((IConfigurePersistence) Activator.CreateInstance(type)).Configure();
            
            return config;
        }



    }

    public class PersistenceDefinition
    {

    }

    public class PersistenceConfiguration
    {
    }

    /// <summary>
    /// Configures the given transport using the default settings
    /// </summary>
    public interface IConfigurePersistence
    {
        void Configure();
    }


    /// <summary>
    /// The generic counterpart to IConfigureTransports
    /// </summary>
    public interface IConfigurePersistence<T> : IConfigurePersistence where T : PersistenceDefinition { }
}
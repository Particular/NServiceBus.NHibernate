namespace NServiceBus.Features
{
    using System;
    using NHibernate.Cfg;

    class SharedMappings
    {
        public void AddMapping(Action<Configuration> mapping)
        {
            mappings.Add(mapping);
        }


        public void ApplyTo(Configuration configuration)
        {
            mappings.ForEach(m=>m(configuration));
        }


        System.Collections.Generic.List<Action<Configuration>> mappings = new System.Collections.Generic.List<Action<Configuration>>();
    }
}
namespace NServiceBus.Features
{
    /// <summary>
    /// Overwrites the default value of NHibernate.Common.ShareConnection in case of SQL Server Transport
    /// </summary>
    class DisabledSharedConnectionByDefault : Feature
    {
        public DisabledSharedConnectionByDefault()
        {            
            EnableByDefault();
            Defaults(s => s.SetDefault("NHibernate.Common.ShareConnection", false));
            DependsOn("SqlServerTransport");
            DependsOn<NHibernateOutboxStorage>();
            DependsOn<NHibernateStorageSession>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            //NOOP
        }
    }
}
namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using NServiceBus.Settings;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSqlConnectionBehavior : IBehavior<IncomingContext>
    {
        public SessionFactoryProvider SessionFactoryProvider { get; set; }
        public ReadOnlySettings Settings { get; set; }

        public string ConnectionString { get; set; }
        public bool DisableConnectionSharing { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            IDbConnection existingConnection;

            if (!DisableConnectionSharing && context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
            {
                next();
                return;
            }

            var lazyConnection = new Lazy<IDbConnection>(() => SessionFactoryProvider.SessionFactory.GetConnection());

            context.Set(string.Format("LazySqlConnection-{0}", ConnectionString), lazyConnection);
            try
            {
                next();
            }
            finally
            {
                if (lazyConnection.IsValueCreated)
                {
                    lazyConnection.Value.Dispose();
                }

                context.Remove(string.Format("LazySqlConnection-{0}", ConnectionString));
            }
        }

        public class Registration : RegisterStep
        {
            public Registration(): base("OpenSqlConnection", typeof(OpenSqlConnectionBehavior), "Makes sure that there is an IDbConnection available on the pipeline")
            {
                InsertAfter(WellKnownStep.CreateChildContainer);
                InsertBeforeIfExists("OutboxDeduplication");
                InsertBefore(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}
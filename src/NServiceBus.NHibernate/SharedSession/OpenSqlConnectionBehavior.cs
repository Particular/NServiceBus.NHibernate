namespace NServiceBus.NHibernate.SharedSession
{
    using System;
    using System.Data;
    using Internal;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSqlConnectionBehavior : IBehavior<IncomingContext>
    {
        public ISessionFactoryProvider SessionFactoryProvider { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            IDbConnection existingConnection;

            if (context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
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

        public class Registration : RegisterBehavior
        {
            public Registration(): base("OpenSqlConnection", typeof(OpenSqlConnectionBehavior), "Makes sure that there is an IDbConnection available on the pipeline")
            {
                InsertAfter(WellKnownBehavior.ChildContainer);
                InsertBeforeIfExists("OutboxDeduplication");
                InsertBefore(WellKnownBehavior.UnitOfWork);
            }
        }
    }
}
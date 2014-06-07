namespace NServiceBus.NHibernate.SharedSession
{
    using System;
    using System.Data;
    using global::NHibernate;
    using Internal;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSessionBehavior : IBehavior<IncomingContext>
    {
        public ISessionFactoryProvider SessionFactoryProvider { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            ISession existingSession;

            //if we already have a session don't interfer
            if (context.TryGet(string.Format("LazyNHibernateSession-{0}", ConnectionString), out existingSession))
            {
                next();
                return;
            }

            IDbConnection existingConnection;
            Lazy<IDbConnection> lazyExistingConnection;

            if (context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
            {
                InnerInvoke(context, next, () => existingConnection);
            }
            else if (context.TryGet(string.Format("LazySqlConnection-{0}", ConnectionString), out lazyExistingConnection))
            {
                InnerInvoke(context, next, () => lazyExistingConnection.Value);
            }
            else
            {
                var lazyConnection = new Lazy<IDbConnection>(() => SessionFactoryProvider.SessionFactory.GetConnection());

                context.Set(string.Format("LazySqlConnection-{0}", ConnectionString), lazyConnection);
                try
                {
                    InnerInvoke(context, next, () => lazyConnection.Value);
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
        }

        void InnerInvoke(BehaviorContext context, Action next, Func<IDbConnection> connectionRetriever)
        {
            var lazySession = new Lazy<ISession>(() =>
            {
                var session = SessionFactoryProvider.SessionFactory.OpenSession(connectionRetriever());
                session.FlushMode = FlushMode.Never;

                return session;
            });

            context.Set(string.Format("LazyNHibernateSession-{0}", ConnectionString), lazySession);
            try
            {
                next();

                if (lazySession.IsValueCreated)
                {
                    lazySession.Value.Flush();
                }
            }
            finally
            {
                if (lazySession.IsValueCreated)
                {
                    lazySession.Value.Dispose();
                }

                context.Remove(string.Format("LazyNHibernateSession-{0}", ConnectionString));
            }
        }

        public class Registration : RegisterBehavior
        {
            public Registration()
                : base("OpenNHibernateSession", typeof(OpenSessionBehavior), "Makes sure that there is a NHibernate ISession available on the pipeline")
            {
                InsertAfter(WellKnownBehavior.UnitOfWork);
                InsertAfterIfExists("OutboxDeduplication");
                InsertBeforeIfExists("OutboxRecorder");
                InsertBefore("OpenNHibernateTransaction");
            }
        }
    }
}
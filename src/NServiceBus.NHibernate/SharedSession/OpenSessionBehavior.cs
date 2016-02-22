namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using System.Transactions;
    using global::NHibernate;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSessionBehavior : IBehavior<IncomingContext>
    {
        public SessionFactoryProvider SessionFactoryProvider { get; set; }
        public string ConnectionString { get; set; }
        public Func<ISessionFactory, string, ISession> SessionCreator { get; set; }
        public bool DisableConnectionSharing { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            ISession existingSession;

            //if we already have a session don't interfer
            if (context.TryGet(LazyNHibernateSessionKey, out existingSession))
            {
                next();
                return;
            }

            IDbConnection existingConnection;
            Lazy<IDbConnection> lazyExistingConnection;

            if (!DisableConnectionSharing && context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
            {
                InnerInvoke(context, next, () => existingConnection);
            }
            else if (context.TryGet(LazySqlConnectionKey, out lazyExistingConnection))
            {
                InnerInvoke(context, next, () => lazyExistingConnection.Value);
            }
            else
            {
                var lazyConnection = new Lazy<IDbConnection>(() => SessionFactoryProvider.SessionFactory.GetConnection());

                context.Set(LazySqlConnectionKey, lazyConnection);
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

                    context.Remove(LazySqlConnectionKey);
                }
            }
        }

        void InnerInvoke(BehaviorContext context, Action next, Func<IDbConnection> connectionRetriever)
        {
            var lazySession = new Lazy<ISession>(() =>
            {
                var session = SessionCreator != null
                    ? SessionCreator(SessionFactoryProvider.SessionFactory, ConnectionString)
                    : SessionFactoryProvider.SessionFactory.OpenSession(connectionRetriever());

                session.FlushMode = FlushMode.Never;

                if (Transaction.Current == null)
                {
                    var tx = session.BeginTransaction();
                    context.Set(LazyNHibernateTransactionKey, tx);
                }
                return session;
            });

            context.Set(LazyNHibernateSessionKey, lazySession);
            try
            {
                next();

                if (lazySession.IsValueCreated)
                {
                    lazySession.Value.Flush();
                    ITransaction tx;
                    if (context.TryGet(LazyNHibernateTransactionKey, out tx))
                    {
                        tx.Commit();
                        tx.Dispose();
                    }
                }
            }
            finally
            {
                if (lazySession.IsValueCreated)
                {
                    ITransaction tx;
                    if (context.TryGet(LazyNHibernateTransactionKey, out tx))
                    {
                        tx.Dispose();
                    }
                    lazySession.Value.Dispose();
                }

                context.Remove(LazyNHibernateSessionKey);
            }
        }

        string LazySqlConnectionKey
        {
            get { return string.Format("LazySqlConnection-{0}", ConnectionString); }
        }

        string LazyNHibernateTransactionKey
        {
            get { return string.Format("LazyNHibernateTransaction-{0}", ConnectionString); }
        }

        string LazyNHibernateSessionKey
        {
            get { return string.Format("LazyNHibernateSession-{0}", ConnectionString); }
        }


        public class Registration : RegisterStep
        {
            public Registration()
                : base("OpenNHibernateSession", typeof(OpenSessionBehavior), "Makes sure that there is a NHibernate ISession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
                InsertAfterIfExists("OutboxDeduplication");
                InsertBefore(WellKnownStep.MutateIncomingTransportMessage);
                InsertBeforeIfExists("OutboxRecorder");
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
            }
        }
    }
}
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
        public ISessionFactory SessionFactory { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            ISession existingSession;

            //if we already have a session don't interfer
            if (context.TryGet(string.Format("NHibernateSession-{0}", ConnectionString), out existingSession))
            {
                next();
                return;
            }

            IDbConnection existingConnection;

            if (context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
            {
                InnerInvoke(context, next, existingConnection);
            }
            else
            {
                using (var connection = SessionFactory.GetConnection())
                {
                    context.Set(string.Format("SqlConnection-{0}", ConnectionString), connection);

                    InnerInvoke(context, next, connection);
                }
            }
        }

        void InnerInvoke(IncomingContext context, Action next, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenSession(connection))
            {
                session.FlushMode = FlushMode.Never;

                context.Set(string.Format("NHibernateSession-{0}", ConnectionString), session);

                next();

                session.Flush();
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
            }
        }
    }
}
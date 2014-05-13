namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using global::NHibernate;
    using Persistence.NHibernate;
    using Pipeline;
    using Pipeline.Contexts;

    class UnitOfWorkBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public ISessionFactory SessionFactory { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            ISession existingSession;

            //if we already have a session don't interfer
            if (context.TryGet(string.Format("NHibernateSession-{0}", ConnectionString), out existingSession))
            {
                next();
                return;
            }

            SqlConnection existingConnection;

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

        void InnerInvoke(ReceivePhysicalMessageContext context, Action next, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenSession(connection))
            {
                session.FlushMode = FlushMode.Never;

                context.Set(string.Format("NHibernateSession-{0}", ConnectionString), session);
                
                next();

                session.Flush();
            }
        }
    }
}
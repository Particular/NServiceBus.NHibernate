namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Data.SqlClient;
    using global::NHibernate;
    using Persistence.NHibernate;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSqlConnectionBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public ISessionFactory SessionFactory { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            SqlConnection existingConnection;

            if (context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
            {
                next();
                return;
            }

            using (var connection = SessionFactory.GetConnection())
            {
                context.Set(string.Format("SqlConnection-{0}", ConnectionString), connection);

                next();
            }
        }
    }
}
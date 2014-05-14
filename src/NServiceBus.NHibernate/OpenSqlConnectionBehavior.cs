namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Data;
    using global::NHibernate;
    using Persistence.NHibernate;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSqlConnectionBehavior : IBehavior<IncomingContext>
    {
        public ISessionFactory SessionFactory { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            IDbConnection existingConnection;

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
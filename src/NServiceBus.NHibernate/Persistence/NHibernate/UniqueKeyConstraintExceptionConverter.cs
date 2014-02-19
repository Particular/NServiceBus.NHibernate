namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data.SqlClient;
    using global::NHibernate.Exceptions;

    class UniqueKeyConstraintExceptionConverter : ISQLExceptionConverter
    {
        public Exception Convert(AdoExceptionContextInfo adoExceptionContextInfo)
        {
            var sqlException = adoExceptionContextInfo.SqlException as SqlException;
            if (sqlException != null)
            {
                // 2601 is unique key, 2627 is unique index; same thing: 
                // http://blog.sqlauthority.com/2007/04/26/sql-server-difference-between-unique-index-vs-unique-constraint/
                if (sqlException.Number == 2601 || sqlException.Number == 2627)
                {
                    return new UniqueKeyException(sqlException.Message);
                }
            }

            return adoExceptionContextInfo.SqlException;
        }
    }

    class UniqueKeyException : Exception
    {
        public UniqueKeyException(string message)
            : base(message)
        {
        }
    }
}
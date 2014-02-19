namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using global::NHibernate.Exceptions;
    using Persistence.NHibernate;

    class SqlLiteUniqueKeyConstraintExceptionConverter : ISQLExceptionConverter
    {
        public Exception Convert(AdoExceptionContextInfo adoExceptionContextInfo)
        {

            if (adoExceptionContextInfo.SqlException.Message.Contains("constraint"))
            {
                return new UniqueKeyException(adoExceptionContextInfo.SqlException.Message);
            }

            return adoExceptionContextInfo.SqlException;
        }
    }
}
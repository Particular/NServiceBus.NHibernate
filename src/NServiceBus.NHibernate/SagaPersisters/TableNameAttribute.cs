namespace NServiceBus.SagaPersisters.NHibernate
{
    using System;

    /// <summary>
    /// Table name to use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Gets or sets the database schema to use for the table.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="TableNameAttribute"/>.
        /// </summary>
        /// <param name="tableName">The table name to use.</param>
        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}
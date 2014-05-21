namespace NServiceBus.SagaPersisters.NHibernate
{
    using System;

    /// <summary>
    /// Marks a property to be used as a versioning column.
    /// </summary>
    /// <remarks>
    /// For more details see http://www.nhforge.org/doc/nh/en/#mapping-declaration-version
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class RowVersionAttribute : Attribute
    {

    }
}
namespace NServiceBus.SagaPersisters.NHibernate.Config.Installer
{
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;
    using Installation.Environments;

    /// <summary>
    /// Installer for <see cref="SagaPersister"/>
    /// </summary>
    public class Installer : INeedToInstallSomething<Windows>
    {
        /// <summary>
        /// <value>true</value> to run installer.
        /// </summary>
        public static bool RunInstaller { get; set; }

        internal static Configuration configuration;

        /// <summary>
        /// Executes the installer.
        /// </summary>
        /// <param name="identity">The user for whom permissions will be given.</param>
        public void Install(string identity)
        {
            if (RunInstaller)
            {
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}

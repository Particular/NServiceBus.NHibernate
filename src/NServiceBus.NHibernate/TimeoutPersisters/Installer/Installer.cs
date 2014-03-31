namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using global::NHibernate.Cfg;
    using Installation;
    using Installation.Environments;

    /// <summary>
    /// Installer for <see cref="TimeoutStorage"/>
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
                new OptimizedSchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}

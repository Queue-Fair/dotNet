//-----------------------------------------------------------------------
// <copyright file="QueueFairConfig.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
// This library uses Newtonsoft.JSON, and won't compile unless you run
//
// > dotnet add package Newtonsoft.JSON
//
// in the root folder of your dotnet project.
namespace QueueFair.Adapter
{
    using Microsoft.Extensions.Logging;

    public class QueueFairConfig
    {
        // Your Account Secret is shown on the Your Account page of
        // the Queue-Fair Portal.  If you change it there, you must
        // change it here too.
        public static string AccountSecret { get; set; } = "REPLACE_WITH_YOUR_ACCOUNT_SECRET";

        // The System Name of your account
        public static string Account { get; set; } = "REPLACE_WITH_YOUR_ACCOUNT_SYSTEM_NAME";

        // Leave this set as is
        public static string FilesServer { get; set; } = "files.queue-fair.net";

        // Time limit for Passed Strings to be considered valid,
        // before and after the current time.  Make sure your system clock is accurately set!
        public static int QueryTimeLimitSeconds { get; set; } = 30;

        // Whether or not to produce logging messages.
        public static bool Debug { get; set; } = false;

        // Set this to "YOUR.IP.ADDRESS" and Debug above to true to restrict debug messages to your IP address.
        public static string DebugIPAddress { get; set; } = null;

        // How long to wait for network reads of config, in seconds
        // or Adapter Server (safe mode only)
        public static int ReadTimeout { get; set; } = 5;

        // How long a cached copy of your Queue-Fair settings will be kept before downloading
        // a fresh copy.  Set this to 0 if you are updating your settings in the
        // Queue-Fair Portal and want to test your changes quickly, but remember
        // to set it back again when you are finished to reduce load on your server.
        public static int SettingsCacheLifetimeMinutes { get; set; } = 5;

        // Whether or not to strip the Passed String from the URL
        // that the Visitor sees on return from the Queue or Adapter servers
        // (simple mode) - when set to true causes one additinal HTTP request
        // to your site but only on the first matching visit from a particular
        // visitor. The recommended value is true.
        public static bool StripPassedString { get; set; } = true;

        // Wheether to send the visitor to the Adapter server for counting (simple mode),
        // or consult the Adapter server (safe mode).  The recommended value is "safe".
        public static string AdapterMode { get; set; } = "safe";

        // On dotNet, a single copy of your Queue-Fair settings is cached in memory.
        public static IQueueFairSettingsSource SettingsSource { get; set; } = new QueueFair.Adapter.RemoteSettingsSource();
    }
}
// Licensed under the MIT license.

using System;
using Garnet.server;
using Microsoft.Extensions.Logging;

namespace Garnet
{
    /// <summary>Hosts Garnet's memory engine and RESP execution without listeners or AOF.</summary>
    public sealed class GarnetEmbeddedServer : GarnetServer
    {
        /// <summary>Creates an embedded engine with explicitly configured memory resources.</summary>
        public GarnetEmbeddedServer(GarnetServerOptions options, ILoggerFactory loggerFactory = null)
            : base(Validate(options), loggerFactory, servers: [])
        {
        }

        /// <summary>Creates an isolated command admission and execution session.</summary>
        public GarnetEmbeddedSession CreateSession() => new(storeWrapper);

        private static GarnetServerOptions Validate(GarnetServerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.EnableAOF || options.EnableCluster || options.EnableStorageTier ||
                options.EnableLua || !options.DisablePubSub || options.MaxDatabases != 1 ||
                options.CompactionFrequencySecs != 0 || options.ExpiredObjectCollectionFrequencySecs != 0 ||
                options.ExpiredKeyDeletionScanFrequencySecs != -1)
                throw new ArgumentException("Embedded ordered execution requires a single memory database, application-owned persistence, and foreground execution.", nameof(options));
            options.QuietMode = true;
            return options;
        }
    }
}

// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Garnet.server;
using Microsoft.Extensions.Logging;
using Tsavorite.core;

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

        /// <summary>Captures the complete engine at an application-owned stable execution boundary.</summary>
        public async ValueTask<Guid> CaptureCheckpointAsync(CancellationToken cancellationToken = default)
        {
            var checkpoint = await storeWrapper.store.TakeFullCheckpointAsync(
                CheckpointType.Snapshot, cancellationToken).ConfigureAwait(false);
            if (!checkpoint.success)
                throw new InvalidOperationException("The engine could not initiate its full checkpoint.");
            return checkpoint.token;
        }

        /// <summary>Restores the exact full checkpoint selected by the application.</summary>
        public ValueTask<long> RestoreCheckpointAsync(Guid token, CancellationToken cancellationToken = default) =>
            storeWrapper.store.RecoverAsync(token, token, cancellationToken: cancellationToken);

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

// Licensed under the MIT license.

using System;

namespace Garnet.server
{
    /// <summary>
    /// Supplies command execution time to synchronous embedded engine operations.
    /// </summary>
    public static class GarnetExecutionTime
    {
        [ThreadStatic]
        private static TimeProvider current;

        /// <summary>Gets the current execution time in UTC ticks.</summary>
        public static long UtcTicks => (current ?? TimeProvider.System).GetUtcNow().UtcTicks;

        /// <summary>Enters a synchronous command's execution clock scope.</summary>
        public static Scope Enter(TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            var previous = current;
            current = timeProvider;
            return new Scope(previous);
        }

        /// <summary>Restores the preceding synchronous execution clock.</summary>
        public readonly struct Scope : IDisposable
        {
            private readonly TimeProvider previous;

            internal Scope(TimeProvider previous) => this.previous = previous;

            /// <inheritdoc/>
            public void Dispose() => current = previous;
        }
    }
}

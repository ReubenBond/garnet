// Licensed under the MIT license.

using System;

namespace Garnet.common
{
    /// <summary>
    /// Supplies command execution time to synchronous embedded engine operations.
    /// </summary>
    public static class GarnetExecutionTime
    {
        [ThreadStatic]
        private static TimeProvider current;

        /// <summary>Gets the current execution time in UTC ticks.</summary>
        public static long UtcTicks => UtcNow.UtcTicks;

        /// <summary>Gets the current command's UTC execution time.</summary>
        public static DateTimeOffset UtcNow => (current ?? TimeProvider.System).GetUtcNow();

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

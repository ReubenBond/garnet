// Licensed under the MIT license.

using System;
using Tsavorite.core;

namespace Garnet.server
{
    /// <summary>
    /// Captures a parsed, authorized command before it executes against the store.
    /// The command memory is borrowed only for this synchronous call.
    /// </summary>
    public interface IGarnetCommandAdmission
    {
        /// <summary>Captures a complete RESP command for later execution.</summary>
        void Admit(RespCommand command, ReadOnlySpan<PinnedSpanByte> arguments, ReadOnlySpan<byte> encodedCommand);
    }
}

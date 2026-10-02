// Licensed under the MIT license.

using System;

namespace Garnet.server
{
    /// <summary>
    /// Captures a parsed, authorized command before it executes against the store.
    /// The command memory is borrowed only for this synchronous call.
    /// </summary>
    public interface IGarnetCommandAdmission
    {
        /// <summary>Captures a complete RESP command for later execution.</summary>
        void Admit(RespCommand command, int argumentCount, ReadOnlySpan<byte> encodedCommand);
    }
}

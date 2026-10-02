// Licensed under the MIT license.

using System;
using Garnet.server;

namespace Garnet
{
    /// <summary>A captured command or a parser/authorization error response.</summary>
    public sealed class GarnetPreparedCommand
    {
        internal GarnetPreparedCommand(RespCommand command, int argumentCount, byte[] request, byte[] error)
        {
            Command = command;
            ArgumentCount = argumentCount;
            Request = request;
            ImmediateResponse = error;
        }

        /// <summary>Gets the parsed command identifier.</summary>
        public RespCommand Command { get; }

        /// <summary>Gets the number of arguments, excluding the command name.</summary>
        public int ArgumentCount { get; }

        /// <summary>Gets the owned command bytes.</summary>
        public ReadOnlyMemory<byte> Request { get; }

        /// <summary>Gets a parser/authorization error or a connection-local response.</summary>
        public ReadOnlyMemory<byte> ImmediateResponse { get; }

        /// <summary>Gets whether admission produced a complete command.</summary>
        public bool IsValid => !Request.IsEmpty;
    }
}

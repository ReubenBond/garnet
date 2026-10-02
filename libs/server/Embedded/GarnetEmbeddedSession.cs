// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Garnet.common;
using Garnet.networking;
using Garnet.server;

namespace Garnet
{
    /// <summary>
    /// Parses commands without applying them and executes committed commands synchronously.
    /// Each session is used by one caller at a time.
    /// </summary>
    public sealed unsafe class GarnetEmbeddedSession : IDisposable
    {
        private readonly CaptureSender sender = new();
        private readonly RespServerSession execution;
        private readonly CommandCapture capture = new();
        private readonly CaptureSender admissionOutput = new();
        private readonly RespServerSession parser;
        private bool disposed;

        /// <summary>Creates a session over an embedded server's store.</summary>
        public GarnetEmbeddedSession(StoreWrapper store)
        {
            execution = new RespServerSession(0, sender, store, null, null, false, trustedExecution: true);
            parser = new RespServerSession(0, admissionOutput, store, null, null, false, commandAdmission: capture);
        }

        /// <summary>Captures one parsed and authorized command before dispatch.</summary>
        public GarnetPreparedCommand Prepare(ReadOnlySpan<byte> request)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            capture.Count = 0;
            capture.Request = null;
            Run(parser, admissionOutput, request);
            if (capture.Count > 1)
                throw new ArgumentException("Admission requires exactly one complete command.", nameof(request));
            return capture.Count == 1
                ? new GarnetPreparedCommand(capture.Command, capture.Arguments, capture.Request, [])
                : new GarnetPreparedCommand(RespCommand.INVALID, 0, [], admissionOutput.GetResult());
        }

        /// <summary>Executes committed RESP bytes, including a complete transaction group.</summary>
        public byte[] Execute(ReadOnlySpan<byte> request, TimeProvider executionTime)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            using var clock = GarnetExecutionTime.Enter(executionTime);
            Run(execution, sender, request);
            return sender.GetResult();
        }

        private static void Run(RespServerSession session, CaptureSender output, ReadOnlySpan<byte> request)
        {
            var buffer = request.ToArray();
            output.Reset();
            fixed (byte* pointer = buffer)
            {
                if (session.TryConsumeMessages(pointer, buffer.Length) != buffer.Length)
                    throw new ArgumentException("A complete RESP command is required.", nameof(request));
            }
            if (output.IsClosed)
                throw new InvalidOperationException("The embedded RESP execution session faulted.");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            execution.Dispose();
            parser.Dispose();
            sender.Dispose();
            admissionOutput.Dispose();
        }

        private sealed class CommandCapture : IGarnetCommandAdmission
        {
            internal int Count;
            internal RespCommand Command;
            internal int Arguments;
            internal byte[] Request;

            public void Admit(RespCommand command, int argumentCount, ReadOnlySpan<byte> encodedCommand)
            {
                Count++;
                Command = command;
                Arguments = argumentCount;
                Request = encodedCommand.ToArray();
            }
        }

        private sealed class CaptureSender : INetworkSender
        {
            private readonly byte[] buffer = GC.AllocateArray<byte>(64 * 1024, pinned: true);
            private readonly ArrayBufferWriter<byte> result = new();
            private readonly MaxSizeSettings sizes = new();

            internal bool IsClosed { get; private set; }
            public MaxSizeSettings GetMaxSizeSettings => sizes;
            public string RemoteEndpointName => "embedded";
            public string LocalEndpointName => "embedded";
            public bool IsLocalConnection() => true;
            public void Enter() { }
            public void Exit() { }
            public void GetResponseObject() { }
            public void ReturnResponseObject() { }
            public void ExitAndReturnResponseObject() { }
            public void SendCallback(object context) { }
            public void Throttle() { }

            public void EnterAndGetResponseObject(out byte* head, out byte* tail)
            {
                head = GetResponseObjectHead();
                tail = head + buffer.Length;
            }

            public byte* GetResponseObjectHead() =>
                (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);

            public byte* GetResponseObjectTail() => GetResponseObjectHead() + buffer.Length;

            public bool SendResponse(int offset, int size)
            {
                result.Write(buffer.AsSpan(offset, size));
                return true;
            }

            public void SendResponse(byte[] value, int offset, int count, object context) =>
                result.Write(value.AsSpan(offset, count));

            internal void Reset() => result.Clear();
            internal byte[] GetResult() => result.WrittenSpan.ToArray();
            public bool TryClose()
            {
                var wasClosed = IsClosed;
                IsClosed = true;
                return !wasClosed;
            }
            public void DisposeNetworkSender(bool waitForSendCompletion) => IsClosed = true;
            public void Dispose() => IsClosed = true;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using System;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    public class NamedPipeServerConnectionListenerFactory :
        IConnectionListenerFactory
    {
        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (!(endpoint is NamedPipeEndPoint namedPipeEndPoint))
            {
                throw new NotSupportedException();
            }

            return new ValueTask<IConnectionListener>(new ConnectionListener(namedPipeEndPoint));
        }

        private sealed class ConnectionListener : IConnectionListener
        {
            private readonly NamedPipeEndPoint _endpoint;

            private NamedPipeServerStream _stream;

            public ConnectionListener(NamedPipeEndPoint endpoint)
            {
                _endpoint = endpoint;

                CreateServerStream();
            }

            public EndPoint EndPoint => _endpoint;

            public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    await _stream.WaitForConnectionAsync(cancellationToken);

                    NamedPipeServerStream connectedStream = _stream;

                    CreateServerStream();

                    return new NamedPipeConnectionContext(connectedStream);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    _stream.Dispose();
                    CreateServerStream();

                    throw;
                }
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }

            private void CreateServerStream()
            {
                _stream = new NamedPipeServerStream(
                    _endpoint.Name,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances);
            }
        }
    }
}

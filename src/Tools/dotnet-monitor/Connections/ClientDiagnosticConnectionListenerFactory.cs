// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ClientDiagnosticConnectionListenerFactory :
        IDiagnosticConnectionListenerFactory
    {
        private readonly ILogger<ClientDiagnosticConnectionListener> _logger;
        private readonly ClientDiagnosticEndPointMonitor _monitor;

        public ClientDiagnosticConnectionListenerFactory(
            ClientDiagnosticEndPointMonitor monitor,
            ILogger<ClientDiagnosticConnectionListener> logger)
        {
            _logger = logger;
            _monitor = monitor;
        }

        public IDiagnosticConnectionListener Bind()
        {
            ClientDiagnosticConnectionListener listener = new(_monitor, _logger);
            listener.Start();
            return listener;
        }
    }
}

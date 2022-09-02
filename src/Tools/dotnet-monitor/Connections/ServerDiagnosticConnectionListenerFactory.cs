// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ServerDiagnosticConnectionListenerFactory :
        IDiagnosticConnectionListenerFactory
    {
        private readonly ILogger<ServerDiagnosticConnectionListener> _logger;
        private readonly DiagnosticPortOptions _portOptions;

        public ServerDiagnosticConnectionListenerFactory(
            IOptions<DiagnosticPortOptions> portOptions,
            ILogger<ServerDiagnosticConnectionListener> logger)
        {
            _logger = logger;
            _portOptions = portOptions.Value;
        }

        public IDiagnosticConnectionListener Bind()
        {
            ServerDiagnosticConnectionListener listener = new(_portOptions.EndpointName, _logger);
            listener.Start(_portOptions.MaxConnections, _portOptions.GetDeleteEndpointOnStartup());
            return listener;
        }
    }
}

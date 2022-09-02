// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Connections
{
    internal sealed class DefaultDiagnosticConnectionListenerFactory :
        IDiagnosticConnectionListenerFactory
    {
        private readonly IOptions<DiagnosticPortOptions> _portOptions;
        private readonly IServiceProvider _serviceProvider;

        public DefaultDiagnosticConnectionListenerFactory(
            IOptions<DiagnosticPortOptions> portOptions,
            IServiceProvider serviceProvider)
        {
            _portOptions = portOptions;
            _serviceProvider = serviceProvider;
        }

        public IDiagnosticConnectionListener Bind()
        {
            return _portOptions.Value.GetConnectionMode() switch
            {
                DiagnosticPortConnectionMode.Connect => Bind<ClientDiagnosticConnectionListenerFactory>(),
                DiagnosticPortConnectionMode.Listen => Bind<ServerDiagnosticConnectionListenerFactory>(),
                _ => throw new NotSupportedException()
            };
        }

        private IDiagnosticConnectionListener Bind<T>() where T : IDiagnosticConnectionListenerFactory
        {
            return _serviceProvider.GetRequiredService<T>().Bind();
        }
    }
}

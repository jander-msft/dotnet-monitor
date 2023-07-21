// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class MonitoringServiceHostedService :
        IHostedService
    {
        private IMonitoringService _service;

        public MonitoringServiceHostedService(IMonitoringService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _service.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _service.StopAsync(cancellationToken);
        }
    }
}

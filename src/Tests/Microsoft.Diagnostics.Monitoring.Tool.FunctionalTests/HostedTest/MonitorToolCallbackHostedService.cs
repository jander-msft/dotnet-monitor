// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal class MonitorToolCallbackHostedService : IHostedService
    {
        private readonly IEnumerable<IMonitorToolCallback> _callbacks;

        public MonitorToolCallbackHostedService(IEnumerable<IMonitorToolCallback> callbacks)
        {
            _callbacks = callbacks;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IMonitorToolCallback callback in _callbacks)
            {
                await callback.OnBeforeShutdownAsync(cancellationToken);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class MonitoringService : IMonitoringService
    {
        private readonly IServiceProvider _serviceProvider;

        public MonitoringService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            ServiceCollection services = new();

            // Configuration
            services.AddSingleton(configuration);

            // Logging
            services.AddSingleton(loggerFactory);
            services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

            _serviceProvider = services.BuildServiceProvider();
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (IMonitoringHostedService service in _serviceProvider.GetServices<IMonitoringHostedService>())
            {
                await service.StartAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IMonitoringHostedService service in _serviceProvider.GetServices<IMonitoringHostedService>())
            {
                await service.StopAsync(cancellationToken);
            }
        }

        public Task<IProcessInfo> GetProcessAsync(ProcessKey? processKey, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IProcessInfo>> GetProcessesAsync(DiagProcessFilter processFilter, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}

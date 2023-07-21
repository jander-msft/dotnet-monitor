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
    internal sealed class MonitoringService :
        IMonitoringService
    {
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IServiceProvider _serviceProvider;

        public MonitoringService(ILoggerFactory loggerFactory, IConfiguration configuration,
            IEndpointInfoSource endpointInfoSource)
        {
            ServiceCollection services = new();

            // Configuration
            services.AddSingleton(configuration);

            // Logging
            services.AddSingleton(loggerFactory);
            services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

            // Default Process
            services.ConfigureDefaultProcess(configuration);

            // Endpoints
            services.AddSingleton(endpointInfoSource);

            // Diagnostics
            services.AddSingleton<IDiagnosticServices, DiagnosticServices>();

            _serviceProvider = services.BuildServiceProvider();

            _diagnosticServices = _serviceProvider.GetRequiredService<IDiagnosticServices>();
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
            return _diagnosticServices.GetProcessAsync(processKey, token);
        }

        public Task<IEnumerable<IProcessInfo>> GetProcessesAsync(DiagProcessFilter processFilter, CancellationToken token)
        {
            return _diagnosticServices.GetProcessesAsync(processFilter, token);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    /// <summary>
    /// Get exception information from default process and store it.
    /// </summary>
    internal sealed class ExceptionsService :
        BackgroundService,
        IDiagnosticLifetimeService
    {
        private readonly IEndpointInfo _endpointInfo;
        private readonly IExceptionsStore _exceptionsStore;
        private readonly IOptions<ExceptionsOptions> _exceptionsOptions;
        private readonly StartupHookService _startupHookeService;

        public ExceptionsService(
            IEndpointInfo endpointInfo,
            IOptions<ExceptionsOptions> exceptionsOptions,
            IExceptionsStore exceptionsStore,
            StartupHookService startupHookService)
        {
            _endpointInfo = endpointInfo;
            _exceptionsStore = exceptionsStore;
            _exceptionsOptions = exceptionsOptions;
            _startupHookeService = startupHookService;
        }

        async ValueTask IDiagnosticLifetimeService.StartAsync(CancellationToken cancellationToken)
        {
            await StartAsync(cancellationToken);
        }

        async ValueTask IDiagnosticLifetimeService.StopAsync(CancellationToken cancellationToken)
        {
            await StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_exceptionsOptions.Value.GetEnabled())
            {
                // Exception history is not enabled
                return;
            }

            if (!await _startupHookeService.GetResultAsync(stoppingToken))
            {
                // Startup hook was not applied
                return;
            }

            DiagnosticsClient client = new(_endpointInfo.Endpoint);

            EventExceptionsPipelineSettings settings = new();
            await using EventExceptionsPipeline pipeline = new(client, settings, _exceptionsStore);

            // Monitor for exceptions
            await pipeline.RunAsync(stoppingToken);
        }
    }
}

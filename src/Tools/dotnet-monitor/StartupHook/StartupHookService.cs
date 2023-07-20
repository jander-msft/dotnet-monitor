// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookService : IDiagnosticLifetimeService
    {
        private readonly IEndpointInfo _endpointInfo;
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly TaskCompletionSource<bool> _resultTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly StartupHookValidator _startupHookValidator;

        public StartupHookService(
            IEndpointInfo endpointInfo,
            IInProcessFeatures inProcessFeatures,
            StartupHookValidator startupHookValidator)
        {
            _endpointInfo = endpointInfo;
            _inProcessFeatures = inProcessFeatures;
            _startupHookValidator = startupHookValidator;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (_inProcessFeatures.IsStartupHookRequired)
            {
                if (await _startupHookValidator.CheckEnvironmentAsync(_endpointInfo, cancellationToken))
                {
                    _resultTaskSource.SetResult(true);
                    return;
                }

                if (await _startupHookValidator.ApplyStartupHook(_endpointInfo, cancellationToken))
                {
                    _resultTaskSource.SetResult(true);
                    return;
                }

                await _startupHookValidator.CheckEnvironmentAsync(_endpointInfo, cancellationToken, logInstructions: true);
            }
            _resultTaskSource.SetResult(false);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public Task<bool> GetResultAsync(CancellationToken cancellationToken)
        {
            return _resultTaskSource.Task.WaitAsync(cancellationToken);
        }
    }
}

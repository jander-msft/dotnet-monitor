// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal sealed class ToolTargetValidation : ITestValidation
    {
        private readonly Func<ApiClient, IMonitorTarget, CancellationToken, Task> _callback;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MonitorToolHostedService _monitorToolService;
        private readonly ITestOutputHelper _outputHelper;
        private readonly MonitorTargetHostedService _monitorTargetService;

        public ToolTargetValidation(
            ITestOutputHelper outputHelper,
            MonitorToolHostedService monitorToolService,
            MonitorTargetHostedService monitorTargetService,
            IHttpClientFactory httpClientFactory,
            Func<ApiClient, IMonitorTarget, CancellationToken, Task> callback)
        {
            _callback = callback;
            _httpClientFactory = httpClientFactory;
            _monitorTargetService = monitorTargetService;
            _monitorToolService = monitorToolService;
            _outputHelper = outputHelper;
        }

        public async Task ValidateAsync(CancellationToken cancellationToken)
        {
            HttpClient httpClient = await _monitorToolService.CreateDefaultHttpClientAsync(_httpClientFactory, cancellationToken);

            ApiClient apiClient = new(_outputHelper, httpClient);

            await _monitorTargetService.ExecuteAsync((target, token) => _callback(apiClient, target, token), cancellationToken);
        }
    }
}

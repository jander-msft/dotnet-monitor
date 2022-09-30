// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal abstract class ToolValidationBase
    {
        private readonly Func<ApiClient, CancellationToken, Task> _callback;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MonitorToolHostedService _monitorToolService;
        private readonly ITestOutputHelper _outputHelper;

        protected ToolValidationBase(
            ITestOutputHelper outputHelper,
            MonitorToolHostedService monitorToolService,
            IHttpClientFactory httpClientFactory,
            Func<ApiClient, CancellationToken, Task> callback)
        {
            _callback = callback;
            _httpClientFactory = httpClientFactory;
            _monitorToolService = monitorToolService;
            _outputHelper = outputHelper;
        }

        protected async Task InvokeAsync(CancellationToken cancellationToken)
        {
            HttpClient httpClient = await _monitorToolService.CreateDefaultHttpClientAsync(_httpClientFactory, cancellationToken);

            ApiClient apiClient = new(_outputHelper, httpClient);

            await _callback(apiClient, cancellationToken);
        }
    }
}

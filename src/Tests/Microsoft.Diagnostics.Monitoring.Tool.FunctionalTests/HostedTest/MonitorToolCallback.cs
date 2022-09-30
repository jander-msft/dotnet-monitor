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
    internal sealed class MonitorToolCallback : ToolValidationBase, IMonitorToolCallback
    {
        public MonitorToolCallback(
            ITestOutputHelper outputHelper,
            MonitorToolHostedService monitorToolService,
            IHttpClientFactory httpClientFactory,
            Func<ApiClient, CancellationToken, Task> callback)
            : base(outputHelper, monitorToolService, httpClientFactory, callback)
        {
        }

        public Task OnBeforeShutdownAsync(CancellationToken cancellationToken)
        {
            return InvokeAsync(cancellationToken);
        }
    }
}

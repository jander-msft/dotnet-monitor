// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class TraceTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public TraceTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, TraceProfile.Cpu)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, TraceProfile.Cpu)]
#endif
        public async Task TraceCpuTest(DiagnosticPortConnectionMode mode, TraceProfile profile)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            AppRunner appRunner = new(_outputHelper);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.BusyWait.Name;

            await appRunner.ExecuteAsync(async () =>
            {
                ProcessInfo processInfo = await apiClient.GetProcessAsync(appRunner.ProcessId);
                Assert.NotNull(processInfo);

                using ResponseStreamHolder holder = await apiClient.CaptureTraceAsync(appRunner.ProcessId, profile, durationSeconds: 10, metricsIntervalSeconds: 1);
                Assert.NotNull(holder);

                await holder.Stream.CopyToAsync(Stream.Null);

                await appRunner.SendCommandAsync(TestAppScenarios.BusyWait.Commands.Continue);
            });
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class MetricsTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public MetricsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that turning off metrics via the command line will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public Task DisableMetricsViaCommandLineTest()
        {
            return ValidateDisabledMetrics(toolOptions =>
            {
                toolOptions.CommandLine.EnableMetrics = false;
            });
        }

        /// <summary>
        /// Tests that turning off metrics via configuration will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public Task DisableMetricsViaEnvironmentTest()
        {
            return ValidateDisabledMetrics(toolOptions =>
            {
                toolOptions.ConfigureEnvironment(rootOptions =>
                {
                    rootOptions.DisableMetrics();
                });
            });
        }

        /// <summary>
        /// Tests that turning off metrics via settings will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public Task DisableMetricsViaSettingsTest()
        {
            return ValidateDisabledMetrics(toolOptions =>
            {
                toolOptions.ConfigureSettings(rootOptions =>
                {
                    rootOptions.DisableMetrics();
                });
            });
        }

        /// <summary>
        /// Tests that turning off metrics via key-per-file will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public Task DisableMetricsViaKeyPerFileTest()
        {
            return ValidateDisabledMetrics(toolOptions =>
            {
                toolOptions.ConfigureKeyPerFile(rootOptions =>
                {
                    rootOptions.DisableMetrics();
                });
            });
        }

        private Task ValidateDisabledMetrics(Action<MonitorToolTestOptions> configure)
        {
            return TestHostBuilder.Create(_outputHelper)
                .UseDiagnosticPort(DiagnosticPortConnectionMode.Connect)
                .AddMonitorTool(_httpClientFactory, (options, context) =>
                {
                    configure(options);
                })
                .AddToolValidation(async (client, token) =>
                {
                    // Check that /metrics does not serve metrics
                    var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                        () => client.GetMetricsAsync());
                    Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
                })
                .ExecuteAsync(CancellationToken.None);
        }
    }
}

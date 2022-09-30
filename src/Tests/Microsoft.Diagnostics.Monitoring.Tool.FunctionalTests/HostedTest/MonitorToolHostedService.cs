// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal class MonitorToolHostedService : IHostedService
    {
        private readonly IOptions<MonitorToolTestOptions> _options;
        private readonly MonitorCollectRunner _runner;

        public MonitorToolHostedService(
            IOptions<MonitorToolTestOptions> options,
            ITestOutputHelper outputHelper)
        {
            _options = options;
            _runner = new(outputHelper);
        }

        public async Task<HttpClient> CreateDefaultHttpClientAsync(IHttpClientFactory factory, CancellationToken token)
        {
            string address = await _runner.GetDefaultAddressAsync(token);

            return await _runner.CreateHttpClientAsync(factory, address, Extensions.Options.Options.DefaultName, token);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            MonitorToolTestOptions options = _options.Value;
            if (!string.IsNullOrEmpty(options.CommandLine.DiagnosticPort))
            {
                _runner.DiagnosticPortPath = options.CommandLine.DiagnosticPort;
                _runner.ConnectionModeViaCommandLine = WebApi.DiagnosticPortConnectionMode.Listen;
            }

            if (options.CommandLine.NoAuth.GetValueOrDefault(false))
            {
                _runner.DisableAuthentication = true;
            }

            if (options.CommandLine.EnableMetrics.HasValue)
            {
                _runner.DisableMetricsViaCommandLine = !options.CommandLine.EnableMetrics.Value;
            }

            if (null != options.ConfigurationFromEnvironment)
            {
                _runner.UseConfigurationFromEnvironment(options.ConfigurationFromEnvironment);
            }

            if (null != options.ConfigurationFromKeyPerFile)
            {
                _runner.WriteKeyPerValueConfiguration(options.ConfigurationFromKeyPerFile);
            }

            if (null != options.ConfigurationFromSettings)
            {
                await _runner.WriteUserSettingsAsync(options.ConfigurationFromSettings);
            }

            await _runner.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _runner.DisposeAsync();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.HostedTest;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class MonitorTargetAppRunner :
        IMonitorTargetRunner,
        IMonitorTarget
    {
        private readonly MonitorTargetTestOptions _options;
        private readonly AppRunner _runner;

        public MonitorTargetAppRunner(
            TestBuilderContext context,
            string targetIdentifier,
            IOptionsMonitor<MonitorTargetTestOptions> options,
            ITestOutputHelper outputHelper)
        {
            _options = options.Get(targetIdentifier);
            _runner = new(outputHelper, context.TestAssembly, _options.TargetIdentifier, _options.TargetFrameworkMoniker);
        }

        public Task SendCommandAsync(string name, CancellationToken cancellationToken)
        {
            return _runner.SendCommandAsync(name, cancellationToken);
        }

        public async Task<IMonitorTarget> StartAsync(CancellationToken cancellationToken)
        {
            _runner.ScenarioName = _options.ScenarioName;

            foreach ((string key, string value) in _options.Environment)
            {
                _runner.Environment.Add(key, value);
            }

            if (!string.IsNullOrEmpty(_options.DiagnosticPorts))
            {
                _runner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
                _runner.DiagnosticPortPath = _options.DiagnosticPorts;
            }

            await _runner.StartAsync(cancellationToken);

            ProcessId = await _runner.ProcessIdTask;

            return this;
        }

        public async Task<int> StopAsync(CancellationToken cancellationToken)
        {
            await _runner.DisposeAsync();

            return _runner.ExitCode;
        }

        public int ProcessId { get; private set; }
    }
}

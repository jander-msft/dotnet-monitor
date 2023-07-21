// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class EndpointUtilities
    {
        private readonly ITestOutputHelper _outputHelper;

        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);

        public EndpointUtilities(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public AppRunner CreateAppRunner(Assembly testAssembly, string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, testAssembly, appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        public static async Task VerifyConnectionAsync(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(await runner.ProcessIdTask, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }
    }

    internal sealed class ServerSourceHolder : IAsyncDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public ServerSourceHolder(IServiceProvider serviceProvider, ServerEndpointInfoSource source, string transportName)
        {
            _serviceProvider = serviceProvider;

            Source = source;
            TransportName = transportName;
        }

        public ServerEndpointInfoSource Source { get; }

        public string TransportName { get; }

        public async ValueTask DisposeAsync()
        {
            await Source.StopAsync(CancellationToken.None);

            Source.Dispose();

            await DisposableHelper.DisposeAsync(_serviceProvider);
        }
    }
}

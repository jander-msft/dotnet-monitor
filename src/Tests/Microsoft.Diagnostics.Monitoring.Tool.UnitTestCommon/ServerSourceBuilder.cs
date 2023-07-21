// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class ServerSourceBuilder
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly ServiceCollection _services = new();
        private readonly string _transportName;

        public ServerSourceBuilder(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out _transportName);
            _outputHelper.WriteLine("Building server endpoint info source at '" + _transportName + "'.");

            _services.AddSingleton<ScopedEndpointInfo>();
            _services.AddSingleton(Extensions.Options.Options.Create(
                new DiagnosticPortOptions()
                {
                    ConnectionMode = DiagnosticPortConnectionMode.Listen,
                    EndpointName = _transportName
                }));
            _services.AddSingleton<ServerEndpointInfoSource>();
        }

        public ServerSourceBuilder AddCallback(IEndpointInfoSourceCallbacks callbacks)
        {
            _services.AddSingleton(callbacks);

            return this;
        }

        public ServerSourceBuilder AddOperationTracker(OperationTrackerService trackerService)
        {
            _services.AddSingleton(trackerService);

            return AddCallback(new OperationTrackerServiceEndpointInfoSourceCallback(trackerService));
        }

        public async Task<ServerSourceHolder> BuildAndStartAsync(CancellationToken cancellationToken = default)
        {
            IServiceProvider sp = _services.BuildServiceProvider();

            ServerEndpointInfoSource server = sp.GetRequiredService<ServerEndpointInfoSource>();

            _outputHelper.WriteLine("Starting server endpoint info source at '" + _transportName + "'.");

            await server.StartAsync(cancellationToken);

            return new ServerSourceHolder(sp, server, _transportName);
        }

        public static Task<ServerSourceHolder> CreateAndStartAsync(
            ITestOutputHelper outputHelper,
            CancellationToken cancellationToken = default)
        {
            ServerSourceBuilder builder = new(outputHelper);
            return builder.BuildAndStartAsync(cancellationToken);
        }

        public static Task<ServerSourceHolder> CreateAndStartAsync(
            ITestOutputHelper outputHelper,
            IEndpointInfoSourceCallbacks callbacks,
            CancellationToken cancellationToken = default)
        {
            ServerSourceBuilder builder = new(outputHelper);
            builder.AddCallback(callbacks);
            return builder.BuildAndStartAsync(cancellationToken);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class KestrelServerEndpointInfoSource : IpcEndpointInfoSource
    {
        private readonly KestrelServer _server;

        public KestrelServerEndpointInfoSource(string name, ILoggerFactory loggerFactory)
        {
            KestrelServerOptions options = new();
            options.Listen(new NamedPipeEndPoint() { Name = name }, listenOptions => listenOptions.Use(OnConnection));

            IConnectionListenerFactory factory = new NamedPipeServerConnectionListenerFactory();

            _server = new(Options.Create(options), factory, loggerFactory);
        }

        public override async Task<IEndpointInfo> AcceptAsync(CancellationToken token)
        {
            await Task.Delay(-1, token);

            return null;
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public override void Remove(IEndpointInfo info)
        {
        }

        public Task StartAsync()
        {
            return _server.StartAsync(new Application(), CancellationToken.None);
        }

        private ConnectionDelegate OnConnection(ConnectionDelegate connectionDelegate)
        {
            return context => { return Task.CompletedTask; };
        }

        private sealed class Application : IHttpApplication<object>
        {
            public object CreateContext(IFeatureCollection contextFeatures)
            {
                return null;
            }

            public void DisposeContext(object context, Exception exception)
            {
            }

            public Task ProcessRequestAsync(object context)
            {
                return Task.CompletedTask;
            }
        }
    }
}

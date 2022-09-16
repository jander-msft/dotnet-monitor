﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Aggregates diagnostic endpoints that are established at a transport path via a reversed server.
    /// </summary>
    internal sealed class EndpointInfoSource : BackgroundService
    {
        // The number of items that the pending removal channel will hold before forcing
        // the writer to wait for capacity to be available.
        private const int PendingRemovalChannelCapacity = 1000;

        // The amount of time to wait when checking if the a endpoint info should be
        // pruned from the list of endpoint infos. If the runtime doesn't have a viable connection within
        // this time, it will be pruned from the list.
        private static readonly TimeSpan PruneWaitForConnectionTimeout = TimeSpan.FromMilliseconds(250);

        // The amount of time to wait between pruning operations.
        private static readonly TimeSpan PruningInterval = TimeSpan.FromSeconds(3);

        private readonly List<IEndpointInfo> _activeEndpoints = new();
        private readonly SemaphoreSlim _activeEndpointsSemaphore = new(1);

        private readonly ChannelReader<IEndpointInfo> _pendingRemovalReader;
        private readonly ChannelWriter<IEndpointInfo> _pendingRemovalWriter;

        private readonly CancellationTokenSource _cancellation = new();
        private readonly IEnumerable<IEndpointInfoSourceCallbacks> _callbacks;
        private readonly DiagnosticPortOptions _portOptions;

        private readonly OperationTrackerService _operationTrackerService;
        private readonly ILogger<EndpointInfoSource> _logger;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Constructs a <see cref="EndpointInfoSource"/> that aggregates diagnostic endpoints
        /// from a reversed diagnostics server at path specified by <paramref name="portOptions"/>.
        /// </summary>
        public EndpointInfoSource(
            IOptions<DiagnosticPortOptions> portOptions,
            IEnumerable<IEndpointInfoSourceCallbacks> callbacks = null,
            OperationTrackerService operationTrackerService = null,
            ILogger<EndpointInfoSource> logger = null,
            ILoggerFactory loggerFactory = null)
        {
            _callbacks = callbacks ?? Enumerable.Empty<IEndpointInfoSourceCallbacks>();
            _operationTrackerService = operationTrackerService;
            _portOptions = portOptions.Value;
            _logger = logger;
            _loggerFactory = loggerFactory;

            BoundedChannelOptions channelOptions = new(PendingRemovalChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true
            };
            Channel<IEndpointInfo> pendingRemovalChannel = Channel.CreateBounded<IEndpointInfo>(channelOptions);
            _pendingRemovalReader = pendingRemovalChannel.Reader;
            _pendingRemovalWriter = pendingRemovalChannel.Writer;
        }

        public override void Dispose()
        {
            base.Dispose();

            _pendingRemovalWriter.TryComplete();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using IpcEndpointInfoSource source = _portOptions.GetConnectionMode() switch
            {
                DiagnosticPortConnectionMode.Connect => await StartClientSourceAsync(),
                DiagnosticPortConnectionMode.Listen => await StartServerSourceAsync(),
                _ => throw new NotSupportedException()
            };

            await Task.WhenAll(
                ListenAsync(source, stoppingToken),
                MonitorEndpointsAsync(stoppingToken),
                NotifyAndRemoveAsync(source, stoppingToken)
                );
        }

        /// <summary>
        /// Gets the list of <see cref="IpcEndpointInfo"/> served from the reversed diagnostics server.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A list of active <see cref="IEndpointInfo"/> instances.</returns>
        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token)
        {
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, _cancellation.Token);

            List<IEndpointInfo> validEndpoints = new();
            await PruneEndpointsAsync(validEndpoints, linkedSource.Token);
            return validEndpoints;
        }

        private async Task<IpcEndpointInfoSource> StartClientSourceAsync()
        {
            IpcClientEndpointInfoSource source = new(_logger);
            await source.StartAsync();
            return source;
        }

        private async Task<IpcEndpointInfoSource> StartServerSourceAsync()
        {
            KestrelServerEndpointInfoSource source = new(_portOptions.EndpointName, _loggerFactory);
            await source.StartAsync();
            //IpcServerEndpointInfoSource source = new(_portOptions.EndpointName, _logger);
            //await source.StartAsync(_portOptions.MaxConnections, _portOptions.DeleteEndpointOnStartup.GetValueOrDefault(DiagnosticPortOptionsDefaults.DeleteEndpointOnStartup));
            return source;
        }

        /// <summary>
        /// Accepts endpoint infos from the reversed diagnostics server.
        /// </summary>
        /// <param name="maxConnections">The maximum number of connections the server will support.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        private async Task ListenAsync(IpcEndpointInfoSource source, CancellationToken token)
        {
            // Continuously accept endpoint infos from the reversed diagnostics server so
            // that <see cref="ReversedDiagnosticsServer.AcceptAsync(CancellationToken)"/>
            // is always awaited in order to to handle new runtime instance connections
            // as well as existing runtime instance reconnections.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IEndpointInfo info = await source.AcceptAsync(token).ConfigureAwait(false);

                    _ = Task.Run(() => ResumeAndQueueEndpointInfo(source, info, token), token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task ResumeAndQueueEndpointInfo(IpcEndpointInfoSource source, IEndpointInfo endpointInfo, CancellationToken token)
        {
            try
            {
                foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                {
                    await callback.OnBeforeResumeAsync(endpointInfo, token).ConfigureAwait(false);
                }

                // Send ResumeRuntime message for runtime instances that connect to the server. This will allow
                // those instances that are configured to pause on start to resume after the diagnostics
                // connection has been made. Instances that are not configured to pause on startup will ignore
                // the command and return success.
                var client = new DiagnosticsClient(endpointInfo.Endpoint);
                try
                {
                    await client.ResumeRuntimeAsync(token);
                }
                catch (ServerErrorException)
                {
                    // The runtime likely doesn't understand the ResumeRuntime command.
                }

                await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    _activeEndpoints.Add(endpointInfo);

                    foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                    {
                        await callback.OnAddedEndpointInfoAsync(endpointInfo, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _activeEndpointsSemaphore.Release();
                }
            }
            catch (Exception)
            {
                source?.Remove(endpointInfo);

                throw;
            }
        }

        private async Task MonitorEndpointsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(PruningInterval);

                await PruneEndpointsAsync(validEndpoints: null, token);
            }
        }

        private async Task NotifyAndRemoveAsync(IpcEndpointInfoSource source, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                IEndpointInfo endpoint = await _pendingRemovalReader.ReadAsync(token);

                foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                {
                    await callback.OnRemovedEndpointInfoAsync(endpoint, token).ConfigureAwait(false);
                }

                source.Remove(endpoint);
            }
        }

        private async Task PruneEndpointsAsync(List<IEndpointInfo> validEndpoints, CancellationToken token)
        {
            // Prune connections that no longer have an active runtime instance before
            // returning the list of connections.
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                // Check the transport for each endpoint info and remove it if the check fails.
                List<Task<bool>> checkTasks = new();
                foreach (IEndpointInfo info in _activeEndpoints)
                {
                    checkTasks.Add(Task.Run(() => CheckEndpointAsync(info, token), token));
                }

                // Wait for all checks to complete
                bool[] results = await Task.WhenAll(checkTasks).ConfigureAwait(false);

                // Remove failed endpoints from active list; record the failed endpoints
                // for removal after releasing the active endpoints semaphore.
                int endpointIndex = 0;
                for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
                {
                    IEndpointInfo endpoint = _activeEndpoints[endpointIndex];
                    if (results[resultIndex])
                    {
                        validEndpoints?.Add(endpoint);
                        endpointIndex++;
                    }
                    else
                    {
                        _activeEndpoints.RemoveAt(endpointIndex);

                        await _pendingRemovalWriter.WriteAsync(endpoint, token);
                    }
                }
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }

        /// <summary>
        /// Tests the endpoint to see if its connection is viable.
        /// </summary>
        private async Task<bool> CheckEndpointAsync(IEndpointInfo info, CancellationToken token)
        {
            // If a dump operation is in progress, the runtime is likely to not respond to
            // diagnostic requests. Do not check for responsiveness while the dump operation
            // is in progress.
            if (_operationTrackerService?.IsExecutingOperation(info) == true)
            {
                return true;
            }

            using var timeoutSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            try
            {
                timeoutSource.CancelAfter(PruneWaitForConnectionTimeout);

                await info.Endpoint.WaitForConnectionAsync(linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                _logger?.EndpointTimeout(info.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}

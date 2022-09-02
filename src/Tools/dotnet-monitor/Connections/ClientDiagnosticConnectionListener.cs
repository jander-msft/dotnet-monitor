// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ClientDiagnosticConnectionListener :
        IDiagnosticConnectionListener
    {
        // The amount of time to wait before abandoning the attempt to create an EndpointInfo from
        // the enumerated processes. This may happen if a runtime instance is unresponsive to
        // diagnostic pipe commands. Give a generous amount of time, but not too long since a single
        // unresponsive process will cause all HTTP requests to be delayed by the timeout period.
        private static readonly TimeSpan AbandonProcessTimeout = TimeSpan.FromSeconds(3);

        private readonly ILogger<ClientDiagnosticConnectionListener> _logger;
        private readonly Channel<DiagnosticEndPoint> _endPointChannel = CreateEndPointChannel();
        private readonly ChannelWriter<DiagnosticEndPoint> _endPointWriter;
        private readonly ChannelReader<DiagnosticEndPoint> _endPointReader;

        private readonly ClientDiagnosticEndPointMonitor _monitor;

        public ClientDiagnosticConnectionListener(
            ClientDiagnosticEndPointMonitor monitor,
            ILogger<ClientDiagnosticConnectionListener> logger)
        {
            _logger = logger;

            _monitor = monitor;

            _endPointWriter = _endPointChannel.Writer;
            _endPointReader = _endPointChannel.Reader;
        }

        public async Task<IEndpointInfo> AcceptAsync(CancellationToken token)
        {
            IEndpointInfo endpointInfo = null;
            while (null == endpointInfo)
            {
                DiagnosticEndPoint endPoint = await _endPointReader.ReadAsync(token).ConfigureAwait(false);

                using CancellationTokenSource timeoutTokenSource = new(AbandonProcessTimeout);
                using CancellationTokenSource linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token);

                CancellationToken timeoutToken = timeoutTokenSource.Token;

                try
                {
                    endpointInfo = await EndpointInfo.FromDiagnosticEndPoint(endPoint, timeoutToken).ConfigureAwait(false);
                }
                // Catch when timeout on waiting for EndpointInfo creation. Some runtime instances may be
                // in a bad state and not respond to any requests on their diagnostic pipe; gracefully abandon
                // waiting for these processes.
                catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
                {
                    _logger.DiagnosticRequestCancelled(endPoint.ProcessId);
                }
                // Catch all other exceptions and log them.
                catch (Exception ex)
                {
                    _logger.DiagnosticRequestFailed(endPoint.ProcessId, ex);
                }
            }

            return endpointInfo;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void Remove(IEndpointInfo info)
        {
        }

        public void Start()
        {
            _monitor.OnRemoved(OnRemoved);
            _monitor.OnAdded(OnAdded);
        }

        private void OnAdded(DiagnosticEndPoint endPoint)
        {
            _logger.ObservedDiagnosticPortCreated(endPoint.Path);

            _endPointWriter.TryWrite(endPoint);
        }

        private void OnRemoved(DiagnosticEndPoint endPoint)
        {
            _logger.ObservedDiagnosticPortDeleted(endPoint.Path);
        }

        private static Channel<DiagnosticEndPoint> CreateEndPointChannel()
        {
            BoundedChannelOptions options = new(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            };

            return Channel.CreateBounded<DiagnosticEndPoint>(options);
        }
    }
}

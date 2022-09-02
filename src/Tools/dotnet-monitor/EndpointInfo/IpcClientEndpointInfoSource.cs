// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class IpcClientEndpointInfoSource : IpcEndpointInfoSource
    {
        // The amount of time to wait before abandoning the attempt to create an EndpointInfo from
        // the enumerated processes. This may happen if a runtime instance is unresponsive to
        // diagnostic pipe commands. Give a generous amount of time, but not too long since a single
        // unresponsive process will cause all HTTP requests to be delayed by the timeout period.
        private static readonly TimeSpan AbandonProcessTimeout = TimeSpan.FromSeconds(3);

        private static readonly Regex DiagnosticPortNameRegex = new(PidIpcEndpoint.DiagnosticsPortPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        private readonly FileSystemWatcher _diagnosticPortWatcher = new(PidIpcEndpoint.IpcRootPath);
        private readonly ILogger _logger;
        private readonly Channel<int> _newProcessChannel = CreateNewProcessChannel();
        private readonly ChannelWriter<int> _newProcessWriter;
        private readonly ChannelReader<int> _newProcessReader;
        private readonly HashSet<int> _visitedProcessIds = new HashSet<int>();

        public IpcClientEndpointInfoSource(ILogger logger)
        {
            _logger = logger;

            _diagnosticPortWatcher.NotifyFilter = NotifyFilters.FileName;
            _diagnosticPortWatcher.Created += Watcher_Created;
            _diagnosticPortWatcher.Deleted += Watcher_Deleted;
            _diagnosticPortWatcher.Filter = "dotnet-diagnostic-*";

            _newProcessWriter = _newProcessChannel.Writer;
            _newProcessReader = _newProcessChannel.Reader;
        }

        public override async Task<IEndpointInfo> AcceptAsync(CancellationToken token)
        {
            IEndpointInfo endpointInfo = null;
            while (null == endpointInfo)
            {
                int processId = await _newProcessReader.ReadAsync(token).ConfigureAwait(false);

                using CancellationTokenSource timeoutTokenSource = new(AbandonProcessTimeout);
                using CancellationTokenSource linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token);

                CancellationToken timeoutToken = timeoutTokenSource.Token;

                try
                {
                    endpointInfo = await EndpointInfo.FromProcessIdAsync(processId, token).ConfigureAwait(false);
                }
                // Catch when timeout on waiting for EndpointInfo creation. Some runtime instances may be
                // in a bad state and not respond to any requests on their diagnostic pipe; gracefully abandon
                // waiting for these processes.
                catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
                {
                    _logger.DiagnosticRequestCancelled(processId);
                    return null;
                }
                // Catch all other exceptions and log them.
                catch (Exception ex)
                {
                    _logger.DiagnosticRequestFailed(processId, ex);
                    return null;
                }
            }

            return endpointInfo;
        }

        public override ValueTask DisposeAsync()
        {
            _diagnosticPortWatcher.EnableRaisingEvents = false;
            _diagnosticPortWatcher.Dispose();

            return ValueTask.CompletedTask;
        }

        public override void Remove(IEndpointInfo info)
        {
            Console.WriteLine("Request Remove: {0}", info.ProcessId);

            _visitedProcessIds.Remove(info.ProcessId);
        }

        public Task StartAsync()
        {
            _diagnosticPortWatcher.EnableRaisingEvents = true;

            foreach (string path in Directory.GetFiles(PidIpcEndpoint.IpcRootPath))
            {
                PublishIfDiagnosticPort(path);
            }

            return Task.CompletedTask;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Diag Pipe Created: {0}", e.FullPath);

            PublishIfDiagnosticPort(e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Diag Pipe Deleted: {0}", e.FullPath);
        }

        private void PublishIfDiagnosticPort(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            Match match = DiagnosticPortNameRegex.Match(fileInfo.Name);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var processId) &&
                    _visitedProcessIds.Add(processId))
                {
                    _newProcessWriter.TryWrite(processId);
                }
            }
        }

        private static Channel<int> CreateNewProcessChannel()
        {
            BoundedChannelOptions options = new(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            };

            return Channel.CreateBounded<int>(options);
        }
    }
}

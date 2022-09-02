// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ServerDiagnosticConnectionListener :
        IDiagnosticConnectionListener
    {
        private readonly string _endpointName;
        private readonly ILogger<ServerDiagnosticConnectionListener> _logger;
        private readonly ReversedDiagnosticsServer _server;

        private IDisposable _watcher;

        public ServerDiagnosticConnectionListener(
            string endpointName,
            ILogger<ServerDiagnosticConnectionListener> logger)
        {
            _endpointName = endpointName;
            _logger = logger;
            _server = new ReversedDiagnosticsServer(endpointName);
        }

        public async Task<IEndpointInfo> AcceptAsync(CancellationToken token)
        {
            IpcEndpointInfo endpointInfo = await _server.AcceptAsync(token);

            return await EndpointInfo.FromIpcEndpointInfoAsync(endpointInfo, token);
        }

        public ValueTask DisposeAsync()
        {
            return _server.DisposeAsync();
        }

        public void Remove(IEndpointInfo info)
        {
            _server.RemoveConnection(info.RuntimeInstanceCookie);
        }

        public void Start(int? maxConnections = null, bool deleteOnStartup = true)
        {
            if (deleteOnStartup)
            {
                DeleteOnStartup();
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_endpointName));
            }

            _server.Start(maxConnections.GetValueOrDefault(ReversedDiagnosticsServer.MaxAllowedConnections));

            _watcher = SetupPortWatcher();
        }

        private void DeleteOnStartup()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                File.Exists(_endpointName))
            {
                // In some circumstances stale files from previous instances of dotnet-monitor cause
                // the new instance to fail binding. We need to delete the file in this situation.
                try
                {
                    _logger.DiagnosticPortDeleteAttempt(_endpointName);

                    File.Delete(_endpointName);
                }
                catch (Exception ex)
                {
                    _logger.DiagnosticPortDeleteFailed(_endpointName, ex);
                }
            }
        }

        private IDisposable SetupPortWatcher()
        {
            // If running on Windows, a named pipe is used so there is no need to watch it.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            FileSystemWatcher watcher = null;
            try
            {
                watcher = new(Path.GetDirectoryName(_endpointName));
                void onDiagnosticPortAltered()
                {
                    _logger.DiagnosticPortAlteredWhileInUse(_endpointName);
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                    }
                    catch
                    {
                    }
                }

                watcher.Filter = Path.GetFileName(_endpointName);
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Deleted += (_, _) => onDiagnosticPortAltered();
                watcher.Renamed += (_, _) => onDiagnosticPortAltered();
                watcher.Error += (object _, ErrorEventArgs e) => _logger.DiagnosticPortWatchingFailed(_endpointName, e.GetException());
                watcher.EnableRaisingEvents = true;

                return watcher;
            }
            catch (Exception ex)
            {
                _logger.DiagnosticPortWatchingFailed(_endpointName, ex);
                watcher?.Dispose();
            }

            return null;
        }
    }
}

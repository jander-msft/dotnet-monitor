// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ClientDiagnosticEndPointMonitor :
        IDiagnosticEndPointMonitor,
        IDisposable
    {
        private readonly Dictionary<string, DiagnosticEndPoint> _endPointMap = new();
        private readonly FileSystemWatcher _watcher = new(PidIpcEndpoint.IpcRootPath);

        private EventHandler<DiagnosticEndPoint> _onAdded;
        private EventHandler<DiagnosticEndPoint> _onRemoved;

        public ClientDiagnosticEndPointMonitor()
        {
            _watcher.NotifyFilter = NotifyFilters.FileName;
            _watcher.Created += Watcher_Created;
            _watcher.Deleted += Watcher_Deleted;
            _watcher.Filter = "dotnet-diagnostic-*";
            _watcher.EnableRaisingEvents = true;

            foreach (string path in Directory.GetFiles(PidIpcEndpoint.IpcRootPath))
            {
                if (DiagnosticEndPoint.TryParse(path, out DiagnosticEndPoint endPoint))
                {
                    _endPointMap.Add(path, endPoint);
                }
            }
        }

        public void OnAdded(Action<DiagnosticEndPoint> callback)
        {
            _onAdded += (s, e) => callback(e);

            foreach (DiagnosticEndPoint endPoint in _endPointMap.Values)
            {
                callback(endPoint);
            }
        }

        public void OnRemoved(Action<DiagnosticEndPoint> callback)
        {
            _onRemoved += (s, e) => callback(e);
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (DiagnosticEndPoint.TryParse(e.FullPath, out DiagnosticEndPoint endPoint))
            {
                _endPointMap.Add(e.FullPath, endPoint);

                _onAdded(this, endPoint);
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (_endPointMap.Remove(e.FullPath, out DiagnosticEndPoint endPoint))
            {
                _onRemoved(this, endPoint);
            }
        }
    }
}

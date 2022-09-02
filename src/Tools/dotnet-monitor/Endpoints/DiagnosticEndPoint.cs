// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticEndPoint : EndPoint
    {
        private static readonly Regex DiagnosticPortNameRegex = new(PidIpcEndpoint.DiagnosticsPortPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public string Path { get; }

        public int ProcessId { get; }

        public DiagnosticEndPoint(string path, int processId)
        {
            Path = path;
            ProcessId = processId;
        }

        public override AddressFamily AddressFamily => AddressFamily.Unknown;

        public override EndPoint Create(SocketAddress socketAddress)
        {
            throw new NotSupportedException();
        }

        public override SocketAddress Serialize()
        {
            throw new NotSupportedException();
        }

        public static bool TryParse(string path, out DiagnosticEndPoint endPoint)
        {
            FileInfo fileInfo = new FileInfo(path);

            Match match = DiagnosticPortNameRegex.Match(fileInfo.Name);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int processId))
                {
                    endPoint = new(path, processId);
                    return true;
                }
            }

            endPoint = null;
            return false;
        }
    }
}

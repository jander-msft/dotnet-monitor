// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class DiagnosticPortSupplier : IDisposable
    {
        private readonly TemporaryDirectory _tempDirectory;

        public DiagnosticPortSupplier(
            ITestOutputHelper outputHelper,
            DiagnosticPortConnectionMode toolConnectionMode)
        {
            _tempDirectory = new(outputHelper);

            ToolConnectionMode = toolConnectionMode;

            if (toolConnectionMode == DiagnosticPortConnectionMode.Connect)
            {
                TargetConnectionMode = DiagnosticPortConnectionMode.Listen;
            }
            else
            {
                TargetConnectionMode = DiagnosticPortConnectionMode.Connect;

                string fileName = Guid.NewGuid().ToString("D");
                ToolDiagnosticPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    fileName : Path.Combine(_tempDirectory.FullName, fileName);
            }
        }

        public void Dispose()
        {
            _tempDirectory.Dispose();
        }

        public DiagnosticPortConnectionMode TargetConnectionMode { get; }

        public string ToolDiagnosticPort { get; }

        public DiagnosticPortConnectionMode ToolConnectionMode { get; }
    }
}

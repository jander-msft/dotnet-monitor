// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal sealed class MonitorToolTestOptions
    {
        public MonitorToolCommandLineOptions CommandLine { get; set; } = new();

        public RootOptions ConfigurationFromEnvironment { get; set; }

        public RootOptions ConfigurationFromKeyPerFile { get; set; }

        public RootOptions ConfigurationFromSettings { get; set; }
    }

    internal sealed class MonitorToolCommandLineOptions
    {
        public string DiagnosticPort { get; set; }

        public bool? EnableMetrics { get; set; }

        public bool? NoAuth { get; set; }
    }
}

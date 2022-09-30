// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class MonitorTargetTestOptions
    {
        public string DiagnosticPorts { get; set; }

        public IDictionary<string, string> Environment { get; set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string ScenarioName { get; set; }

        public TargetFrameworkMoniker TargetFrameworkMoniker { get; set; }

        public string TargetIdentifier { get; set; }
    }
}

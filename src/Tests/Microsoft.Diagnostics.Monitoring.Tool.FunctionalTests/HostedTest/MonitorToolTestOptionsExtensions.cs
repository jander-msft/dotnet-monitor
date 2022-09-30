// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor;
using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal static class MonitorToolTestOptionsExtensions
    {
        public static MonitorToolTestOptions ConfigureEnvironment(this MonitorToolTestOptions options, Action<RootOptions> configure)
        {
            options.ConfigurationFromEnvironment = ConfigureRootOptions(options.ConfigurationFromEnvironment, configure);

            return options;
        }

        public static MonitorToolTestOptions ConfigureKeyPerFile(this MonitorToolTestOptions options, Action<RootOptions> configure)
        {
            options.ConfigurationFromKeyPerFile = ConfigureRootOptions(options.ConfigurationFromKeyPerFile, configure);

            return options;
        }

        public static MonitorToolTestOptions ConfigureSettings(this MonitorToolTestOptions options, Action<RootOptions> configure)
        {
            options.ConfigurationFromSettings = ConfigureRootOptions(options.ConfigurationFromSettings, configure);

            return options;
        }

        private static RootOptions ConfigureRootOptions(RootOptions options, Action<RootOptions> configure)
        {
            if (null == options)
            {
                options = new RootOptions();
            }

            configure(options);

            return options;
        }
    }
}

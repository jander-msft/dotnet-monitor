// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static partial class TargetFrameworkExtensions
    {
        public static Version GetAspNetCoreFrameworkVersion(this TargetFramework moniker)
        {
            return ParseVersionRemoveLabel(moniker.GetAspNetCoreFrameworkVersionString());
        }

        public static string GetAspNetCoreFrameworkVersionString(this TargetFramework moniker)
        {
            switch (moniker)
            {
                case TargetFramework.Current:
                    return TestDotNetHost.CurrentAspNetCoreVersionString;
                case TargetFramework.NetCoreApp31:
                    return TestDotNetHost.AspNetCore31VersionString;
                case TargetFramework.Net50:
                    return TestDotNetHost.AspNetCore50VersionString;
                case TargetFramework.Net60:
                    return TestDotNetHost.AspNetCore60VersionString;
                case TargetFramework.Net70:
                    return TestDotNetHost.AspNetCore70VersionString;
                case TargetFramework.Net80:
                    return TestDotNetHost.AspNetCore80VersionString;
            }
            throw CreateUnsupportedException(moniker);
        }

        public static string GetNetCoreAppFrameworkVersionString(this TargetFramework moniker)
        {
            switch (moniker)
            {
                case TargetFramework.Current:
                    return TestDotNetHost.CurrentNetCoreVersionString;
                case TargetFramework.NetCoreApp31:
                    return TestDotNetHost.NetCore31VersionString;
                case TargetFramework.Net50:
                    return TestDotNetHost.NetCore50VersionString;
                case TargetFramework.Net60:
                    return TestDotNetHost.NetCore60VersionString;
                case TargetFramework.Net70:
                    return TestDotNetHost.NetCore70VersionString;
                case TargetFramework.Net80:
                    return TestDotNetHost.NetCore80VersionString;
            }
            throw CreateUnsupportedException(moniker);
        }

        // Checks if the specified moniker is the same as the test value or if it is Current
        // then matches the same TFM for which this assembly was built.
        public static bool IsEffectively(this TargetFramework moniker, TargetFramework test)
        {
            if (TargetFramework.Current == test)
            {
                throw new ArgumentException($"Parameter {nameof(test)} cannot be TargetFramework.Current");
            }

            return moniker == test || (TargetFramework.Current == moniker && TargetFrameworks.CurrentAssembly == test);
        }

        private static Version ParseVersionRemoveLabel(string versionString)
        {
            Assert.NotNull(versionString);
            int prereleaseLabelIndex = versionString.IndexOf('-');
            if (prereleaseLabelIndex >= 0)
            {
                versionString = versionString.Substring(0, prereleaseLabelIndex);
            }
            return Version.Parse(versionString);
        }
    }
}

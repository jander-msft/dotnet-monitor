// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public enum TargetFramework
    {
        Current,
        NetCoreApp31,
        Net50,
        Net60,
        Net70,
        Net80
    }

    public static class TargetFrameworks
    {
        public const TargetFramework CurrentAssembly =
#if NET8_0
            TargetFramework.Net80;
#elif NET7_0
            TargetFramework.Net70;
#elif NET6_0
            TargetFramework.Net60;
#elif NET5_0
            TargetFramework.Net50;
#elif NETCOREAPP3_1
            TargetFramework.NetCoreApp31;
#endif
    }

    public static partial class TargetFrameworkExtensions
    {
        public static string ToFolderName(this TargetFramework moniker)
        {
            switch (moniker)
            {
                case TargetFramework.Net50:
                    return "net5.0";
                case TargetFramework.NetCoreApp31:
                    return "netcoreapp3.1";
                case TargetFramework.Net60:
                    return "net6.0";
                case TargetFramework.Net70:
                    return "net7.0";
                case TargetFramework.Net80:
                    return "net8.0";
            }
            throw CreateUnsupportedException(moniker);
        }

        private static ArgumentException CreateUnsupportedException(TargetFramework moniker)
        {
            return new ArgumentException($"Unsupported target framework moniker: {moniker:G}");
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class TestHostBuilder
    {
        public static IHostBuilder Create(ITestOutputHelper outputHelper)
        {
            Assembly testAssembly = Assembly.GetCallingAssembly();
            HostBuilder hostBuilder = new();
            return hostBuilder.ConfigureServices((context, services) =>
            {
                TestBuilderContext testBuilderContext = new(testAssembly);
                context.Properties.Add(typeof(TestBuilderContext), testBuilderContext);

                services.AddSingleton(outputHelper);
                services.AddSingleton(testBuilderContext);
            });
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.TestBuilder;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.TestBuilder
{
    internal static class MonitorToolTestBuilder
    {
        public static ITestBuilder<ApiClient, MonitorToolTestOptions> Create(ITestOutputHelper outputHelper, IHttpClientFactory httpClientFactory)
        {
            return TestBuilderFactory.Create<MonitorToolFactory, ApiClient, MonitorToolTestOptions>(Assembly.GetExecutingAssembly(), outputHelper)
                .ConfigureServices((context, services) => services.AddSingleton(httpClientFactory));
        }
    }
}

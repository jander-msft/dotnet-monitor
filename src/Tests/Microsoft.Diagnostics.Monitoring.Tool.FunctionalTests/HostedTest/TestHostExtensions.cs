// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal static class TestHostExtensions
    {
        public static IHostBuilder AddToolBeforeShutdownValidation(this IHostBuilder hostBuilder, Func<ApiClient, CancellationToken, Task> callback)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMonitorToolCallback>(sp => new MonitorToolCallback(
                    sp.GetRequiredService<ITestOutputHelper>(),
                    sp.GetRequiredService<MonitorToolHostedService>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    callback));
            });
        }

        public static IHostBuilder AddTargetValidation(this IHostBuilder hostBuilder, Func<ApiClient, IMonitorTarget, CancellationToken, Task> callback)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<ITestValidation>(sp => new ToolTargetValidation(
                    sp.GetRequiredService<ITestOutputHelper>(),
                    sp.GetRequiredService<MonitorToolHostedService>(),
                    sp.GetRequiredService<MonitorTargetHostedService>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    callback));
            });
        }

        public static IHostBuilder AddToolValidation(this IHostBuilder hostBuilder, Func<ApiClient, CancellationToken, Task> callback)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<ITestValidation>(sp => new ToolValidation(
                    sp.GetRequiredService<ITestOutputHelper>(),
                    sp.GetRequiredService<MonitorToolHostedService>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    callback));
            });
        }

        public static IHostBuilder AddMonitorTool(this IHostBuilder hostBuilder, IHttpClientFactory httpClientFactory, Action<MonitorToolTestOptions, TestBuilderContext> configure = null)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                OptionsBuilder<MonitorToolTestOptions> optionsBuilder = services
                    .AddOptions<MonitorToolTestOptions>()
                    .Configure((MonitorToolTestOptions options, DiagnosticPortSupplier portSupplier) =>
                    {
                        if (portSupplier.ToolConnectionMode == DiagnosticPortConnectionMode.Listen)
                        {
                            options.CommandLine.DiagnosticPort = portSupplier.ToolDiagnosticPort;
                        }

                        options.CommandLine.NoAuth = true;
                    });

                if (null != configure)
                {
                    optionsBuilder.Configure(configure);
                }

                services.AddSingleton<MonitorToolHostedService>();
                services.AddHostedServiceForwarder<MonitorToolHostedService>();
                services.AddHostedService<MonitorToolCallbackHostedService>();
                services.AddSingleton(httpClientFactory);
            });
        }

        public static void AddHostedServiceForwarder<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService
        {
            services.AddHostedService<THostedService>(sp => sp.GetRequiredService<THostedService>());
        }
    }
}

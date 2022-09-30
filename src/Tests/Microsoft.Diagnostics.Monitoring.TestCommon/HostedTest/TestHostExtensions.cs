// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.HostedTest;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class TestHostExtensions
    {
        public static async Task ExecuteAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken)
        {
            IHost host = hostBuilder.Build();

            try
            {
                await host.StartAsync(cancellationToken);

                IEnumerable<ITestValidation> validations = host.Services.GetRequiredService<IEnumerable<ITestValidation>>();

                foreach (ITestValidation validation in validations)
                {
                    await validation.ValidateAsync(cancellationToken);
                }

                await host.StopAsync(cancellationToken);
            }
            finally
            {
                if (host is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (host is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        public static IHostBuilder AddSendCommandToTargets(this IHostBuilder hostBuilder, string name)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<ITestValidation>(sp => new SendCommandToTargetsValidation(
                    sp.GetRequiredService<MonitorTargetHostedService>(),
                    name));
            });
        }

        public static IHostBuilder UseDiagnosticPort(this IHostBuilder hostBuilder, DiagnosticPortConnectionMode toolConnectionMode)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(sp => new DiagnosticPortSupplier(
                    sp.GetRequiredService<ITestOutputHelper>(),
                    toolConnectionMode));
            });
        }

        public static IHostBuilder AddTargetApp(this IHostBuilder hostBuilder, string scenarioName, Action<MonitorTargetTestOptions, TestBuilderContext> configure = null, int count = 1)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddCommonTargetServices(context);

                while (count > 0)
                {
                    string targetIdentifier = GetTestBuilderContext(context).CreateNextTargetId();

                    services.ConfigureTargetOptions(targetIdentifier, scenarioName, configure);
                    services.AddSingleton<IMonitorTargetRunner>(sp => new MonitorTargetAppRunner(
                        sp.GetRequiredService<TestBuilderContext>(),
                        targetIdentifier,
                        sp.GetRequiredService<IOptionsMonitor<MonitorTargetTestOptions>>(),
                        sp.GetRequiredService<ITestOutputHelper>()));

                    count--;
                }
            });
        }

        public static IHostBuilder AddTargetSimulated(this IHostBuilder hostBuilder, string scenarioName, Action<MonitorTargetTestOptions, TestBuilderContext> configure = null)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                string targetIdentifier = GetTestBuilderContext(context).CreateNextTargetId();

                services.AddCommonTargetServices(context);
                services.ConfigureTargetOptions(targetIdentifier, scenarioName, configure);
            });
        }

        private static IServiceCollection ConfigureTargetOptions(this IServiceCollection services, string targetIdentifier, string scenarioName, Action<MonitorTargetTestOptions, TestBuilderContext> configure)
        {
            OptionsBuilder<MonitorTargetTestOptions> optionsBuilder = services
                .AddOptions<MonitorTargetTestOptions>(targetIdentifier)
                .Configure((MonitorTargetTestOptions options, DiagnosticPortSupplier portSupplier) =>
                {
                    options.TargetFrameworkMoniker = TargetFrameworkMoniker.Current;
                    options.TargetIdentifier = targetIdentifier;
                    if (DiagnosticPortConnectionMode.Connect == portSupplier.TargetConnectionMode)
                    {
                        options.DiagnosticPorts = portSupplier.ToolDiagnosticPort;
                    }
                    options.ScenarioName = scenarioName;
                });

            if (null != configure)
            {
                optionsBuilder.Configure(configure);
            }

            return services;
        }

        private static IServiceCollection AddCommonTargetServices(this IServiceCollection services, HostBuilderContext context)
        {
            if (context.Properties.TryAdd(typeof(MonitorTargetHostedService), null))
            {
                services.AddSingleton<MonitorTargetHostedService>();
                services.AddHostedServiceForwarder<MonitorTargetHostedService>();
            }
            return services;
        }

        private static void AddHostedServiceForwarder<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService
        {
            services.AddHostedService<THostedService>(sp => sp.GetRequiredService<THostedService>());
        }

        private static TestBuilderContext GetTestBuilderContext(HostBuilderContext context)
        {
            return (TestBuilderContext)context.Properties[typeof(TestBuilderContext)];
        }
    }
}

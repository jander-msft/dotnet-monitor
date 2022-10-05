// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.TestBuilder
{
    internal static class TestBuilderExtensions
    {
        public static ITestBuilder<TApi, TOptions> ConfigureOptions<TApi, TOptions>(this ITestBuilder<TApi, TOptions> builder, Action<ITestBuilderContext, TOptions> callback)
        {
            return builder;
        }

        public static async Task ExecuteAsync<TApi, TOptions>(this ITestBuilder<TApi, TOptions> builder, CancellationToken cancellationToken)
        {
            await using ITestExecutor executor = builder.Build();

            await executor.ExecuteAsync(cancellationToken);
        }

        public static ITestBuilder<TApi, TOptions> UseAppTarget<TApi, TOptions>(this ITestBuilder<TApi, TOptions> builder, Action<ITestBuilderContext, MonitorTargetTestOptions> callback = null, int count = 1)
        {
            return builder;
        }

        public static ITestBuilder<TApi, TOptions> UseDiagnosticPort<TApi, TOptions>(this ITestBuilder<TApi, TOptions> builder, DiagnosticPortConnectionMode mode)
        {
            return builder;
        }

        public static ITestBuilder<TApi, TOptions> UseScenario<TApi, TOptions>(this ITestBuilder<TApi, TOptions> builder, Action<ITestBuilderContext, IScenarioBuilder<TApi, TOptions>> callback)
        {
            return builder;
        }
    }
}

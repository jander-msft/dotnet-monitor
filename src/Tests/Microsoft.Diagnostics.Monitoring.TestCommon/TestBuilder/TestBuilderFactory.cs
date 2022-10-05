// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.TestBuilder
{
    internal static class TestBuilderFactory
    {
        public static ITestBuilder<TApi, TOptions> Create<TFactory, TApi, TOptions>(Assembly testAssembly, ITestOutputHelper outputHelper) where TFactory : class, IToolFactory<TApi, TOptions>
        {
            return new TestBuilder<TApi, TOptions>(testAssembly)
                .ConfigureServices((context, services) =>
                 {
                     services.AddSingleton(outputHelper);
                     services.AddSingleton<IToolFactory<TApi, TOptions>, TFactory>();
                 });
        }
    }
}

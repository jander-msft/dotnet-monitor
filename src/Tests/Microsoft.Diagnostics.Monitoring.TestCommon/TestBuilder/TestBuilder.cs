// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.TestBuilder
{
    internal sealed class TestBuilder<TApi, TOptions> : ITestBuilder<TApi, TOptions>
    {
        private readonly List<Action<ITestBuilderContext, IServiceCollection>> _callbacks = new();
        private readonly Assembly _testAssembly;

        public TestBuilder(Assembly testAssembly)
        {
            _testAssembly = testAssembly ?? throw new ArgumentNullException(nameof(testAssembly));
        }

        public ITestBuilder<TApi, TOptions> ConfigureServices(Action<ITestBuilderContext, IServiceCollection> callback)
        {
            _callbacks.Add(callback ?? throw new ArgumentNullException(nameof(callback)));

            return this;
        }

        public ITestExecutor Build()
        {
            ServiceCollection services = new();
            TestBuilderContext context = new(_testAssembly);

            foreach (Action<ITestBuilderContext, IServiceCollection> callback in _callbacks)
            {
                callback(context, services);
            }

            services.BuildServiceProvider();

            return null;
        }
    }
}

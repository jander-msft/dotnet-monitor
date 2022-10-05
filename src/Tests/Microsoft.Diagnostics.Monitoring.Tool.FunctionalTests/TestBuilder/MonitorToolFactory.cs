// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.TestBuilder
{
    internal sealed class MonitorToolFactory : IToolFactory<ApiClient, MonitorToolTestOptions>
    {
        public ApiClient Create(MonitorToolTestOptions options)
        {
            throw new System.NotImplementedException();
        }
    }
}

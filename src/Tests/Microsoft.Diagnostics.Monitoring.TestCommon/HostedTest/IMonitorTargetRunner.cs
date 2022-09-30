// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.HostedTest
{
    internal interface IMonitorTargetRunner
    {
        Task<IMonitorTarget> StartAsync(CancellationToken cancellationToken);

        Task<int> StopAsync(CancellationToken cancellationToken);
    }
}

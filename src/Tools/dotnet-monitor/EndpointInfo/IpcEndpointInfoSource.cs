// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class IpcEndpointInfoSource : IAsyncDisposable
    {
        public abstract Task<IEndpointInfo> AcceptAsync(CancellationToken token);

        public abstract ValueTask DisposeAsync();

        public abstract void Remove(IEndpointInfo info);
    }
}

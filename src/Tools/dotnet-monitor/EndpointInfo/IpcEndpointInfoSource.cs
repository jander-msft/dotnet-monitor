// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class IpcEndpointInfoSource : IAsyncDisposable
    {
        public abstract Task<IpcEndpointInfo> AcceptAsync(CancellationToken token);

        public abstract ValueTask DisposeAsync();

        public abstract void Remove(IpcEndpointInfo info);
    }
}

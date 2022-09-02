// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class IpcClientEndpointInfoSource : IpcEndpointInfoSource
    {
        public override Task<IpcEndpointInfo> AcceptAsync(CancellationToken token)
        {
            return null;
        }

        public override async ValueTask DisposeAsync()
        {
            await Task.Yield();
        }

        public override void Remove(IpcEndpointInfo info)
        {
        }

        public void Start()
        {
        }
    }
}

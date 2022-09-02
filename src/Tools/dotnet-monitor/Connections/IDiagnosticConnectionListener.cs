﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal interface IDiagnosticConnectionListener : IAsyncDisposable
    {
        Task<IEndpointInfo> AcceptAsync(CancellationToken token);

        void Remove(IEndpointInfo endpointInfo);
    }
}

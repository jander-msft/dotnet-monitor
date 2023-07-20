// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    public interface IDiagnosticLifetimeService
    {
        ValueTask StartAsync(CancellationToken cancellationToken);

        ValueTask StopAsync(CancellationToken cancellationToken);
    }
}

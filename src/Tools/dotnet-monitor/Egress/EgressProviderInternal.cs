// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal class EgressProviderInternal<TOptions> :
        IEgressProviderInternal<TOptions>
    {
        private readonly IEgressProvider<TOptions> _provider;
        private readonly IOptionsMonitor<TOptions> _options;

        public EgressProviderInternal(
            IEgressProvider<TOptions> provider,
            IOptionsMonitor<TOptions> options)
        {
            _provider = provider;
            _options = options;
        }

        public Task<string> EgressAsync(
            string providerName,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            return _provider.EgressAsync(
                _options.Get(providerName),
                action,
                artifactSettings,
                token);
        }

        public Task<string> EgressAsync(
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            return _provider.EgressAsync(
                _options.Get(providerName),
                action,
                artifactSettings,
                token);
        }
    }
}

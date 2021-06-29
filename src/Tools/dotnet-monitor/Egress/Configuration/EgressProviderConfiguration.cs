// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    internal sealed class EgressProviderConfiguration<TOptions> :
        IEgressProviderConfiguration<TOptions>
    {
        public EgressProviderConfiguration(IConfiguration configuration, string providerName)
        {
            ProviderName = providerName;
            Configuration = configuration.GetEgressSection().GetSection(providerName);
        }

        public string ProviderName { get; }

        public IConfiguration Configuration { get; }
    }
}

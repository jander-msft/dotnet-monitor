// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    internal sealed class EgressConfigureNamedOptions<TOptions> : IConfigureNamedOptions<TOptions> where TOptions : class
    {
        private readonly IEgressProviderConfiguration<TOptions> _configuration;

        public EgressConfigureNamedOptions(IEgressProviderConfiguration<TOptions> configuration)
        {
            _configuration = configuration;
        }

        public void Configure(string name, TOptions options)
        {
            _configuration.Configuration.GetSection(name).Bind(options);
        }

        public void Configure(TOptions options)
        {
            throw new NotSupportedException();
        }
    }
}

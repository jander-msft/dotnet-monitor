// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    internal interface IEgressProviderDescriptor
    {
        string ProviderName { get; }

        Type OptionsType { get; }
    }

    internal class EgressProviderDescriptor<TOptions> : IEgressProviderDescriptor
    {
        public EgressProviderDescriptor(string providerName)
        {
            ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        }

        public string ProviderName { get; }

        public Type OptionsType => typeof(TOptions);
    }
}

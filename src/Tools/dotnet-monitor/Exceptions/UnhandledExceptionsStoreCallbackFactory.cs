// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class UnhandledExceptionsStoreCallbackFactory :
        IExceptionsStoreCallbackFactory
    {
        private readonly IEndpointInfo _endpointInfo;

        public UnhandledExceptionsStoreCallbackFactory(IEndpointInfo endpointInfo)
        {
            _endpointInfo = endpointInfo;
        }

        public IExceptionsStoreCallback Create(IExceptionsStore store)
        {
            return new UnhandledExceptionsStoreCallback(_endpointInfo);
        }
    }
}

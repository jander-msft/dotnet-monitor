// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class UnhandledExceptionsStoreCallback :
        ExceptionsStoreCallbackBase
    {
        private readonly IEndpointInfo _endpointInfo;

        public UnhandledExceptionsStoreCallback(IEndpointInfo endpointInfo)
        {
            _endpointInfo = endpointInfo;
        }

        public override void Unhandled(IExceptionInstance instance)
        {
            DiagnosticsClient client = new(_endpointInfo.Endpoint);
            client.SetEnvironmentVariable("", "1");
        }
    }
}

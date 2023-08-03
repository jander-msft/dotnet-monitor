// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ConfiguredExceptionsStoreCallback : ExceptionsStoreCallback
    {
        private readonly IEndpointInfo _endpointInfo;

        public ConfiguredExceptionsStoreCallback(IEndpointInfo endpointInfo)
        {
            _endpointInfo = endpointInfo;
        }

        public override void AfterAdd(IExceptionInstance instance)
        {
            ExceptionAdded?.Invoke(this, instance);
        }

        public override void Unhandled(ulong exceptionId)
        {
            UnhandledException?.Invoke(this, exceptionId);

            DiagnosticsClient client = new(_endpointInfo.Endpoint);
            client.SetEnvironmentVariable("UnblockCrash", "1");
        }

        public event EventHandler<IExceptionInstance> ExceptionAdded;

        public event EventHandler<ulong> UnhandledException;
    }
}
